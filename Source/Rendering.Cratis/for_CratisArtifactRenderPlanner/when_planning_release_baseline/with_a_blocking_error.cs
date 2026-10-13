// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_release_baseline;

public class with_a_blocking_error : given.a_release_baseline_request
{
    void Because() => _plan = _planner.Plan(WithBaseline(_assessment) with { TypedContextContractRevision = uint.MaxValue });
    [Fact] void should_still_refuse() => _plan.Success.ShouldBeFalse();
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_preserve_the_blocking_diagnostic() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-021" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_also_report_the_baseline_warnings() => _plan.Diagnostics.Count(diagnostic => diagnostic.Code.StartsWith("STAGE-BASELINE-", StringComparison.Ordinal)).ShouldEqual(2);
}
#endif
