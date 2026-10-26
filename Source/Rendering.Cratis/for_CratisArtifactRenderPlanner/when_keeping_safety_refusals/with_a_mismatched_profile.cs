// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_keeping_safety_refusals;

public class with_a_mismatched_profile : a_register_project_render_request
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var profile = _request.Profile;
        _plan = _planner.Plan(_request with { Profile = ArtifactRenderProfile.Create("other", profile.TargetVersion, profile.Renderer, profile.RendererVersion, profile.Inputs) });
    }

    [Fact] void should_return_a_refused_plan() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
    [Fact] void should_report_the_profile_mismatch() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-CRATIS-001");
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_not_render_around_the_mismatch() => _plan.Refusals.ShouldBeEmpty();
}
#endif
