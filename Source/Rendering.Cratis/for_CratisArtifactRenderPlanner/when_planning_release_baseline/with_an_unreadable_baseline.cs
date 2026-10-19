// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_release_baseline;

public class with_an_unreadable_baseline : given.a_release_baseline_request
{
    void Because() => _plan = _planner.Plan(WithBaseline(new(ReleaseBaselineAssessmentStatus.Unreadable, _model.Revision, [], "Unsupported schema or another application.")));
    [Fact] void should_succeed() => _plan.Success.ShouldBeTrue();
    [Fact] void should_warn_about_the_unreadable_baseline() => _plan.Diagnostics.WithoutAuthenticatedDefaultNotes().Select(diagnostic => diagnostic.Code).ShouldContainOnly("STAGE-BASELINE-003");
}
#endif
