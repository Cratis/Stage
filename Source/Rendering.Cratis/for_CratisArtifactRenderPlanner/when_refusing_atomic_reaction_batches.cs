// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_atomic_reaction_batches : given.an_atomic_reaction
{
    [Theory]
    [InlineData(ArtifactRenderScopeKind.Application)]
    [InlineData(ArtifactRenderScopeKind.Slice)]
    public void should_refuse_multiple_same_event_production_triggers_without_emitting_artifacts(ArtifactRenderScopeKind kind)
    {
        var reaction = _located.Slice.Reactions.Single();
        var trigger = reaction.Triggers.Single();
        ReplaceAutomation(_located.Slice with { Reactions = [reaction with { Triggers = [trigger, trigger] }] });
        var id = kind == ArtifactRenderScopeKind.Application ? _model.Application.Id : _located.Slice.Id;
        var plan = CratisRendering.Plan(_model, SemanticExecutionPlan.Compile(_model).Plan!, new(kind, id), new("Reactions", "Reactions"));
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-043" && diagnostic.Artifact == reaction.Id && diagnostic.Message.Contains("multiple same-event production triggers", StringComparison.Ordinal));
        plan.Artifacts.ShouldBeEmpty();
    }

    [Fact]
    public void should_defensively_refuse_routed_direct_productions()
    {
        SetContext(Compile("reaction batches atomic\neventsource Account\n  identifier String\n  stream Notes\n    streamId String\n" + Productions));
        var source = _model.Application.EventSources.Single();
        var route = new SemanticCommandRoute(source.Id, source.Streams.Single().Id) { StreamId = SemanticExpression.FromValue(SemanticValue.Text("notes")) };
        var reaction = _located.Slice.Reactions.Single();
        var trigger = reaction.Triggers.Single();
        trigger = trigger with { Produces = [trigger.Produces[0] with { Route = route }, trigger.Produces[1]] };
        var slice = _located.Slice with { Reactions = [reaction with { Triggers = [trigger] }] };

        // Screenplay rejects reaction routes before a validated model reaches Stage. Exercise the defensive guard directly.
        var features = SemanticVersionFeatures.ReactionBatchesInSlice(_model.Application, slice);
        features.ShouldContain(feature => feature.Code == "STAGE-ESM-043" && feature.Artifact == reaction.Id && feature.Message.Contains("routed direct productions", StringComparison.Ordinal));
    }

    [Fact]
    public void should_keep_legacy_same_event_triggers_renderable()
    {
        SetContext(Compile(Productions));
        var reaction = _located.Slice.Reactions.Single();
        var trigger = reaction.Triggers.Single();
        ReplaceAutomation(_located.Slice with { Reactions = [reaction with { Triggers = [trigger, trigger] }] });
        Plan();
        Assert.True(_plan.Success, string.Join(';', _plan.Diagnostics));
    }
}
