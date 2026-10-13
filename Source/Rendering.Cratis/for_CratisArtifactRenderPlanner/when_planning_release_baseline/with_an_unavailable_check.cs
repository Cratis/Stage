// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_release_baseline;

public class with_an_unavailable_check : given.a_release_baseline_request
{
    void Because() => _plan = _planner.Plan(WithBaseline(new(ReleaseBaselineAssessmentStatus.Unavailable, _model.Revision, [], "No authoritative identities.")));
    [Fact] void should_succeed() => _plan.Success.ShouldBeTrue();
    [Fact] void should_warn_that_admitted_revisions_are_unchecked() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly("STAGE-BASELINE-004");
}
#endif
