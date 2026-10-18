// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticAutomationArtifactRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_around_refusals;

public class with_a_slice_level_refusal : given.a_refused_specification
{
    void Establish()
    {
        var model = an_automation.Compile("module Work\n  feature Processing\n    slice Automation FollowUp\n      event Completed\n      reaction Worker\n        when Startup\n          produces Completed\n            for \"work\"\n");
        _request = OptIn(new(
            model,
            SemanticExecutionPlan.Compile(model).Plan!,
            CratisRendering.CreateProfile(model.Application.Name, new("Reactions", "Reactions")),
            new(ArtifactRenderScopeKind.Application, model.Application.Id)));
    }

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_keep_non_specification_refusals_fatal() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
    [Fact] void should_keep_the_reaction_refusal() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-024");
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_not_list_omissions() => _plan.Refusals.ShouldBeEmpty();
}
