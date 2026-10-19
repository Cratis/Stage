// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_release_baseline;

public class without_an_assessment : given.a_release_baseline_request
{
    void Because() => _plan = _planner.Plan(WithBaseline(ReleaseBaselineAssessment.NotSupplied));
    [Fact] void should_add_no_warnings() => _plan.Diagnostics.Where(diagnostic => diagnostic.Severity != global::Cratis.Stage.Contracts.Rendering.ArtifactRenderDiagnosticSeverity.Information).ShouldBeEmpty();
    [Fact] void should_preserve_the_existing_digest() => _plan.Digest.ShouldEqual(_unchecked.Digest);
}
#endif
