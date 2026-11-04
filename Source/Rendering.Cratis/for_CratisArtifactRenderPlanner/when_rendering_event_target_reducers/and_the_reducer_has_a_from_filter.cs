// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_event_target_reducers;

public class and_the_reducer_has_a_from_filter
{
    const string EventSource = """
        eventsource OrdersSource
          id "stored-orders"
          identifier String
          stream Entries
            id "stored-entries"
        """;

    static Task<LoadedSemanticModel> Load(string? filter) => when_rendering_a_pure_reducer.Load(
        EventSource + "\n" + and_a_reducer_publishes_a_public_event.Declaration + "\n" +
        (filter is null
            ? and_a_reducer_publishes_a_public_event.Reducer
            : and_a_reducer_publishes_a_public_event.Reducer.Replace("reducer OrderPublisher => OrderPublished\n", $"reducer OrderPublisher => OrderPublished\n        from {filter}\n", StringComparison.Ordinal)));

    static IReadOnlyList<RenderedFile> Files(ArtifactRenderPlan plan) => [.. plan.Artifacts
        .Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
        .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))];

    static string Reducer(IReadOnlyList<RenderedFile> files) => files.Single(file => file.RelativePath.EndsWith("OrderPublisher.cs", StringComparison.Ordinal)).Content;

    [Theory]
    [InlineData("OrdersSource", false)]
    [InlineData("OrdersSource.Entries", true)]
    public async Task should_render_and_compile_the_stored_source_and_optional_stream_filter(string filter, bool withStream)
    {
        var plan = when_rendering_a_pure_reducer.Plan(await Load(filter), self: true);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var files = Files(plan);
        Assert.Contains(
            withStream ? "[FromEventSource<OrdersSourceEventSource>(OrdersSourceEventSource.Entries)]" : "[EventSourceType(\"stored-orders\")]",
            Reducer(files),
            StringComparison.Ordinal);
        Assert.Empty(RenderedOutput.Errors(files));
        var type = RenderedOutput.Load(files).GetType("Projects.Shop.Sales.Publish.OrderPublisher");
        Assert.NotNull(type);
        if (withStream)
        {
            var attribute = Assert.Single(type.GetCustomAttributes(false), attribute => attribute.GetType().IsGenericType);
            Assert.Equal(typeof(FromEventSourceAttribute<>), attribute.GetType().GetGenericTypeDefinition());
            Assert.Equal("stored-entries", attribute.GetType().GetProperty("Stream")!.GetValue(attribute));
        }
        else
        {
            Assert.Equal("stored-orders", Assert.Single(type.GetCustomAttributes(typeof(EventSourceTypeAttribute), false).Cast<EventSourceTypeAttribute>()).EventSourceType.Value);
        }
    }

    [Fact]
    public async Task should_render_no_filter_metadata_when_unfiltered()
    {
        var plan = when_rendering_a_pure_reducer.Plan(await Load(null), self: true);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var reducer = Reducer(Files(plan));
        Assert.DoesNotContain("EventSourceType", reducer, StringComparison.Ordinal);
        Assert.DoesNotContain("FromEventSource", reducer, StringComparison.Ordinal);
    }

    [Fact]
    public async Task should_render_the_filter_in_slice_scope()
    {
        var loaded = await Load("OrdersSource.Entries");
        var slice = loaded.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => !slice.Reducers.IsEmpty);
        var plan = when_rendering_a_pure_reducer.Plan(loaded, new(ArtifactRenderScopeKind.Slice, slice.Id), self: true);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        Assert.Contains("[FromEventSource<OrdersSourceEventSource>(OrdersSourceEventSource.Entries)]", Reducer(Files(plan)), StringComparison.Ordinal);
        Assert.Contains(plan.Artifacts, artifact => artifact.RelativePath == "EventSources/OrdersSourceEventSource.cs");
    }

    [Fact]
    public async Task should_still_refuse_a_filtered_reducer_with_a_tagged_public_event()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(
            EventSource + "\n" + and_a_reducer_publishes_a_public_event.Declaration.Replace("public event OrderPublished\n", "public event OrderPublished\n        tag \"published\"\n", StringComparison.Ordinal) + "\n" +
            and_a_reducer_publishes_a_public_event.Reducer.Replace("reducer OrderPublisher => OrderPublished\n", "reducer OrderPublisher => OrderPublished\n        from OrdersSource\n", StringComparison.Ordinal));
        var plan = when_rendering_a_pure_reducer.Plan(loaded);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-032");
        Assert.DoesNotContain(plan.Artifacts, artifact => artifact.RelativePath.EndsWith("OrderPublisher.cs", StringComparison.Ordinal));
    }
}
#endif
