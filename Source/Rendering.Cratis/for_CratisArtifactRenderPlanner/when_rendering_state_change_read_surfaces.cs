// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_state_change_read_surfaces : Specification
{
    [Fact]
    public void should_render_the_canonical_snapshot_projection_query_and_debug_specifications()
    {
        var model = SemanticModelSerializer.Deserialize(EventRoutesCorpus.CompositesV8.EsmBytes.AsSpan());
        var slice = model.Application.Modules.Single().Features.Single().Slices.Single();
        Assert.Equal(SemanticSliceKind.StateChange, slice.Kind);
        Assert.Equal("Snapshot", Assert.Single(slice.ReadModels).Name);
        Assert.NotEmpty(slice.Projections);
        Assert.NotEmpty(slice.Queries);
        Assert.NotEmpty(slice.Specifications);

        var plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("EventRoutes", "EventRoutes"));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var files = when_selecting_self_reducing_rendering.Files(plan);
        var combined = Assert.Single(files, file => Path.GetFileName(file.RelativePath) == "S.cs");
        Assert.Contains("record Book(", combined.Content, StringComparison.Ordinal);
        Assert.Contains("record Booked(", combined.Content, StringComparison.Ordinal);
        Assert.Contains("record Snapshot(", combined.Content, StringComparison.Ordinal);
        Assert.Contains("class SnapshotProjection", combined.Content, StringComparison.Ordinal);
        Assert.Contains(".From<Booked>(", combined.Content, StringComparison.Ordinal);
        Assert.Contains(" SnapshotById(", combined.Content, StringComparison.Ordinal);
        var artifact = plan.Artifacts.Single(artifact => artifact.RelativePath == combined.RelativePath);
        Assert.Equal(
            new[] { slice.Id }.Concat(slice.Commands.Select(command => command.Id)).Concat(slice.Events.Select(@event => @event.Id))
                .Concat(slice.ReadModels.Select(model => model.Id)).Concat(slice.Projections.Select(projection => projection.Id)).Concat(slice.Queries.Select(query => query.Id))
                .Select(id => id.ToString()).Order(StringComparer.Ordinal),
            artifact.Sources.Select(id => id.ToString()).Order(StringComparer.Ordinal));
        foreach (var specification in slice.Specifications)
        {
            var generated = plan.Artifacts.Where(artifact => artifact.Sources.Contains(specification.Id) && artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal)).ToArray();
            Assert.NotEmpty(generated);
        }
        Assert.Empty(RenderedOutput.Errors(files));
    }

    [Fact]
    public async Task should_preserve_command_only_state_change_output()
    {
        var source = when_rendering_a_pure_reducer.Source.Split("    slice StateView Totals", StringSplitOptions.None)[0];
        var loaded = await when_rendering_a_pure_reducer.Load(source);
        var plan = when_rendering_a_pure_reducer.Plan(loaded);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var files = when_selecting_self_reducing_rendering.Files(plan);
        var command = Assert.Single(files, file => file.RelativePath.EndsWith("PlaceOrder.cs", StringComparison.Ordinal));
        Assert.Contains("record PlaceOrder(", command.Content, StringComparison.Ordinal);
        Assert.Contains("record OrderPlaced(", command.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("using Cratis.Arc.Queries.ModelBound;", command.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("using Cratis.Chronicle.ReadModels;", command.Content, StringComparison.Ordinal);
        var profile = CratisRendering.CreateNewApplicationProfile("Projects", new("Projects", "Projects"));
        var self = when_selecting_self_reducing_rendering.Plan(loaded, profile);
        Assert.True(self.Success, string.Join(Environment.NewLine, self.Diagnostics));
        Assert.Equal(command.Content, when_selecting_self_reducing_rendering.Files(self).Single(file => file.RelativePath == command.RelativePath).Content);
        Assert.Empty(RenderedOutput.Errors(files));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_preserve_classic_and_self_reducers_beside_state_change_commands(bool self)
    {
        var source = when_rendering_a_pure_reducer.Source.Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "context.Event.Id", StringComparison.Ordinal);
        var loaded = await when_rendering_a_pure_reducer.Load(source);
        var slice = loaded.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Kind == SemanticSliceKind.StateView);
        var reducer = Assert.Single(slice.Reducers);
        var plan = when_rendering_a_pure_reducer.Plan(loaded, self: self);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var files = when_selecting_self_reducing_rendering.Files(plan);
        Assert.Contains(files, file => file.RelativePath.EndsWith("PlaceOrder.cs", StringComparison.Ordinal) && file.Content.Contains("record PlaceOrder(", StringComparison.Ordinal));
        var combined = Assert.Single(files, file => file.RelativePath.EndsWith("Totals.cs", StringComparison.Ordinal));
        Assert.Contains("record Total(", combined.Content, StringComparison.Ordinal);
        Assert.Contains(" ById(", combined.Content, StringComparison.Ordinal);
        var reducerFile = self ? combined : Assert.Single(files, file => file.RelativePath.EndsWith("Fold.cs", StringComparison.Ordinal));
        Assert.Contains(self ? "public static Total? Reduce(" : "IReducerFor<Total>", reducerFile.Content, StringComparison.Ordinal);
        var sources = plan.Artifacts.Single(artifact => artifact.RelativePath == reducerFile.RelativePath).Sources;
        Assert.Contains(reducer.ReadModel, sources);
        foreach (var transition in reducer.Transitions)
        {
            Assert.Contains(transition.EventContract, sources);
        }
        if (self)
        {
            Assert.DoesNotContain(files, file => file.RelativePath.EndsWith("Fold.cs", StringComparison.Ordinal));
        }
        Assert.Empty(RenderedOutput.Errors(files));
    }
}
#endif
