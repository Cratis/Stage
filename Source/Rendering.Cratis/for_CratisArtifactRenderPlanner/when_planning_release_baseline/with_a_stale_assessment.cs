// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_release_baseline;

public class with_a_stale_assessment : given.a_release_baseline_request
{
    void Because() => _plan = _planner.Plan(WithBaseline(_assessment with { ModelRevision = default }));
    [Fact] void should_succeed() => _plan.Success.ShouldBeTrue();
    [Fact] void should_report_unavailable_instead_of_stale_violations() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly("STAGE-BASELINE-004");
}
#endif
