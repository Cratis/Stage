// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_StageReleaseBaseline.when_checking_release_baseline;

public class and_a_compilation_has_authoritative_sources : given.a_release_baseline
{
    void Because() => _assessment = StageReleaseBaseline.Check(_workspace.Compilation.Value!, _baseline);
    [Fact] void should_check_the_reconstructed_workspace() => _assessment.Status.ShouldEqual(ReleaseBaselineAssessmentStatus.Checked);
    [Fact] void should_report_no_violations() => _assessment.Violations.ShouldBeEmpty();
    [Fact] void should_require_semantic_parity() => _assessment.ModelRevision.ShouldEqual(_workspace.Compilation.Value!.Model.Revision);
}
#endif
