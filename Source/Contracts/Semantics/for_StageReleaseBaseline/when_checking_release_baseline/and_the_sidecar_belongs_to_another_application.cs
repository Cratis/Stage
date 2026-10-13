// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_StageReleaseBaseline.when_checking_release_baseline;

public class and_the_sidecar_belongs_to_another_application : given.a_release_baseline
{
    void Because() => _assessment = StageReleaseBaseline.Check(_workspace, _baseline with { Document = _baseline.Document! with { ApplicationIdentity = ApplicationIdentity.Create("OtherApp").ToString() } });
    [Fact] void should_report_unreadable() => _assessment.Status.ShouldEqual(ReleaseBaselineAssessmentStatus.Unreadable);
    [Fact] void should_not_invent_missing_revisions() => _assessment.Violations.ShouldBeEmpty();
}
#endif
