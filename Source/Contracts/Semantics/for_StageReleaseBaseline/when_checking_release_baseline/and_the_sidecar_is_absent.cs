// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Workspaces;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_StageReleaseBaseline.when_checking_release_baseline;

public class and_the_sidecar_is_absent : given.a_release_baseline
{
    void Because() => _assessment = StageReleaseBaseline.Check(_workspace, ReleaseBaselineStore.ReadBytes(null, _workspace.IdentityCatalog.Application));
    [Fact] void should_report_absence() => _assessment.Status.ShouldEqual(ReleaseBaselineAssessmentStatus.Absent);
    [Fact] void should_report_no_violations() => _assessment.Violations.ShouldBeEmpty();
}
#endif
