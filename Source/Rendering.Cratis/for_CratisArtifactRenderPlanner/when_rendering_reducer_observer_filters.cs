// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_reducer_observer_filters
{
    internal static string Source(string filter) => """
        eventsource OrdersSource
          id "stored-orders"
          identifier Uuid
          stream Entries
            id "stored-entries"
          stream Ignored
            id "stored-ignored"
        """ + "\n" + when_rendering_a_pure_reducer.Source
            .Replace("reducer Fold => Total", $"reducer Fold => Total\n        from {filter}", StringComparison.Ordinal)
            .Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "context.Event.Id", StringComparison.Ordinal);

    internal static Task<LoadedSemanticModel> Load(string filter) => when_rendering_a_pure_reducer.Load(Source(filter));

    [Theory]
    [InlineData("OrdersSource", false)]
    [InlineData("OrdersSource.Entries", true)]
    public async Task should_compile_the_stored_source_and_optional_stream_filter(string filter, bool withStream)
    {
        var plan = when_rendering_a_pure_reducer.Plan(await Load(filter));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var files = Files(plan);
        var reducer = files.Single(file => file.RelativePath.EndsWith("Fold.cs", StringComparison.Ordinal));
        var expected = withStream
            ? "[global::Cratis.Chronicle.EventSources.FromEventSourceAttribute<global::Projects.EventSources.OrdersSourceEventSource>(global::Projects.EventSources.OrdersSourceEventSource.Entries)]"
            : "[global::Cratis.Chronicle.Events.EventSourceTypeAttribute(\"stored-orders\")]";
        Assert.Contains(expected, reducer.Content, StringComparison.Ordinal);
        Assert.Empty(RenderedOutput.Errors(files));
        var type = RenderedOutput.Load(files).GetType("Projects.Orders.Ordering.Totals.Fold")!;
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

    [Theory]
    [InlineData("OrdersSource")]
    [InlineData("OrdersSource.Entries")]
    public async Task should_include_the_source_definition_in_a_reducer_scoped_plan(string filter)
    {
        var loaded = await Load(filter);
        var slice = loaded.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => !slice.Reducers.IsEmpty);
        var plan = when_rendering_a_pure_reducer.Plan(loaded, new(ArtifactRenderScopeKind.Slice, slice.Id));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        Assert.Contains(plan.Artifacts, artifact => artifact.RelativePath == "EventSources/OrdersSourceEventSource.cs");
    }

    [Fact]
    public async Task should_refuse_a_reserved_stored_stream_filter_without_artifacts()
    {
        var loaded = await Load("OrdersSource.Entries");
        // Screenplay rejects All in source, so exercise Stage's ESM admission directly.
        var original = loaded.Model;
        var sources = original.Application.EventSources.Select(source => source with
        {
            Streams = [.. source.Streams.Select(stream => stream with { StreamKind = stream.StreamKind == "stored-entries" ? "All" : stream.StreamKind })]
        });
        var model = ExecutableSemanticModel.Create(original.LanguageVersion, original.SemanticVersion, original.Application with { EventSources = [.. sources] });
        // The rebuilt model has a new revision; carry the unchanged typed contexts over to it.
        var plan = when_rendering_a_pure_reducer.Plan(loaded with
        {
            Model = model,
            Plan = SemanticExecutionPlan.Compile(model).Plan!,
            TypedContextDescriptors = [.. loaded.TypedContextDescriptors.Select(descriptor => descriptor with { ModelRevision = model.Revision })]
        });
        Assert.False(plan.Success);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-030");
        Assert.Empty(plan.Artifacts);
    }

    [Fact]
    public async Task should_admit_read_model_replay_that_preserves_the_filter()
    {
        const string specification = """
              specification CreatingATotal
                when PlaceOrder
                  id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  amount = 20
                then OrderPlaced
                  id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  amount = 20
                then readmodel Total
                  id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  amount = 20
        """;
        var source = Source("OrdersSource.Entries").Replace("    slice StateView Totals", specification + "\n    slice StateView Totals", StringComparison.Ordinal);
        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(source));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        Assert.NotEmpty(plan.Artifacts);
    }

    static RenderedFile[] Files(ArtifactRenderPlan plan) => [.. plan.Artifacts
        .Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
        .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))];
}
#endif
