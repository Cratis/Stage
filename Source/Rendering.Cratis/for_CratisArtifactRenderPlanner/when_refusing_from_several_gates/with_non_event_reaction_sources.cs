// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticAutomationArtifactRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_refusing_from_several_gates;

public class with_non_event_reaction_sources : an_automation
{
    [Theory]
    [InlineData(SemanticReactionTriggerKind.Startup, "STAGE-ESM-024")]
    [InlineData(SemanticReactionTriggerKind.Shutdown, "STAGE-ESM-024")]
    [InlineData(SemanticReactionTriggerKind.Interval, "STAGE-ESM-024")]
    [InlineData(SemanticReactionTriggerKind.Schedule, "STAGE-ESM-024")]
    [InlineData(SemanticReactionTriggerKind.ApplicationTrigger, "STAGE-ESM-024")]
    [InlineData(SemanticReactionTriggerKind.Event, "STAGE-ESM-039")]
    public void should_collect_the_reaction_and_independent_refusals_without_dereferencing_a_non_event(SemanticReactionTriggerKind kind, string code)
    {
        var slice = _located.Slice;
        var reaction = slice.Reactions.Single(candidate => candidate.Name == "Acknowledge");
        var trigger = reaction.Triggers.Single() with { Kind = kind, Source = _model.Application.Id };
        var module = _model.Application.Modules.Single();
        var feature = module.Features.Single();
        var changed = slice with { Reactions = slice.Reactions.Replace(reaction, reaction with { Triggers = [trigger] }) };
        var model = ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, _model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = feature.Slices.Replace(slice, changed) }] }]
        });

        var plan = new CratisArtifactRenderPlanner().Plan(_context.Request with { Model = model, ImplementationRequirements = default });

        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == code && diagnostic.Artifact == reaction.Id);
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-020" && diagnostic.Artifact == model.Application.Id);
        plan.Success.ShouldBeFalse();
        plan.Artifacts.ShouldBeEmpty();
    }
}
