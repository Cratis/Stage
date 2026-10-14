// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_StageReleaseBaseline.when_checking_release_baseline;

public class and_authoritative_assignments_retain_bootstrap_origins : given.a_historical_release_baseline
{
    void Because() => _assessment = StageReleaseBaseline.Check(_workspace.Compilation.Value!, _baseline);
    [Fact] void should_check_explicit_assignments_without_regenerating_them() => _assessment.Status.ShouldEqual(ReleaseBaselineAssessmentStatus.Checked);
    [Fact] void should_preserve_unchanged_revisions() => _assessment.Violations.ShouldBeEmpty();
}
#endif
