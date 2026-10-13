// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_StageReleaseBaseline.when_checking_release_baseline;

public class and_revisions_are_unchanged : given.a_release_baseline
{
    void Because() => _assessment = StageReleaseBaseline.Check(_workspace, _baseline);
    [Fact] void should_check_the_whole_application() => _assessment.Status.ShouldEqual(ReleaseBaselineAssessmentStatus.Checked);
    [Fact] void should_report_no_violations() => _assessment.Violations.ShouldBeEmpty();
    [Fact] void should_carry_the_assessed_revision() => _assessment.ModelRevision.ShouldEqual(_workspace.Compilation.Value!.Model.Revision);
}
#endif
