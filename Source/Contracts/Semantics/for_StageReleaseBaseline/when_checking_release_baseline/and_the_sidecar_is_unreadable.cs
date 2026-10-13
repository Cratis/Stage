// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Workspaces;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_StageReleaseBaseline.when_checking_release_baseline;

public class and_the_sidecar_is_unreadable : given.a_release_baseline
{
    void Because() => _assessment = StageReleaseBaseline.Check(_workspace, ReleaseBaselineStore.ReadBytes(Encoding.UTF8.GetBytes("not json"), _workspace.IdentityCatalog.Application));
    [Fact] void should_report_unreadable() => _assessment.Status.ShouldEqual(ReleaseBaselineAssessmentStatus.Unreadable);
    [Fact] void should_explain_the_failure() => _assessment.Reason.ShouldNotBeNull();
    [Fact] void should_not_invent_violations() => _assessment.Violations.ShouldBeEmpty();
}
#endif
