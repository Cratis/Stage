// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticAutomationArtifactRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_refusing_from_several_gates;

public class with_non_event_reaction_sources : Specification
{
    [Theory]
    [InlineData("when Startup", "STAGE-ESM-024")]
    [InlineData("when Shutdown", "STAGE-ESM-024")]
    [InlineData("every 1 minutes", "STAGE-ESM-024")]
    [InlineData("at 07:30 on Monday", "STAGE-ESM-024")]
    [InlineData("when Signal", "STAGE-ESM-024")]
    [InlineData("when Started", "STAGE-ESM-039")]
    public void should_collect_the_reaction_and_independent_refusals_without_dereferencing_a_non_event(string trigger, string code)
    {
        var model = an_automation.Compile($$"""
            trigger Signal
            module Work
              feature Processing
                slice Automation FollowUp
                  event Started
                    scheduledAt DateTime
                  event Acknowledged
                    scheduledAt DateTime
                  reaction Acknowledge
                    {{trigger}}
                      produces Acknowledged
                        for "acknowledgments"
                        scheduledAt = $context.occurred
            """);
        var execution = SemanticExecutionPlan.Compile(model).Plan!;
        var reaction = model.Application.Modules.Single().Features.Single().Slices.Single().Reactions.Single();
        if (trigger == "when Started")
        {
            // Fault injection: unresolved event references cannot be authored in valid Screenplay.
            typeof(SemanticReactionTrigger).GetField("<Source>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(reaction.Triggers.Single(), model.Application.Id);
        }

        var request = new ArtifactRenderRequest(
            model,
            execution,
            CratisRendering.CreateProfile(model.Application.Name, new("Reactions", "Reactions")),
            new(ArtifactRenderScopeKind.Application, model.Application.Id)) { ImplementationRequirements = default };
        var plan = new CratisArtifactRenderPlanner().Plan(request);

        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == code && diagnostic.Artifact == reaction.Id);
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-020" && diagnostic.Artifact == model.Application.Id);
        plan.Success.ShouldBeFalse();
        plan.Artifacts.ShouldBeEmpty();
    }
}
