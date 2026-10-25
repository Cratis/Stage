// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_release_baseline;

public class with_a_blocking_error : given.a_release_baseline_request
{
    void Because() => _plan = _planner.Plan(WithBaseline(_assessment) with { TypedContextContractRevision = uint.MaxValue });
    [Fact] void should_still_refuse_the_affected_parts() => rendering_refusals.Rejected(_plan, "STAGE-ESM-021");
    [Fact] void should_compile_the_retained_artifacts() => rendering_refusals.Compiles(_plan);
    [Fact] void should_preserve_the_diagnostic_as_a_warning() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-021" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning).ShouldBeTrue();
    [Fact] void should_also_report_the_baseline_warnings() => _plan.Diagnostics.Count(diagnostic => diagnostic.Code.StartsWith("STAGE-BASELINE-", StringComparison.Ordinal)).ShouldEqual(2);
}
#endif
