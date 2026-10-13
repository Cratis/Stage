// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_StageReleaseBaseline.when_checking_release_baseline;

public class and_a_shared_concept_changes_under_historical_revisions : given.a_historical_release_baseline
{
    void Because() => _assessment = StageReleaseBaseline.Check(Changed(HistoricalSource.Replace("Value : String", "Value : Bool", StringComparison.Ordinal)), _baseline);
    [Fact] void should_report_every_affected_revision() => _assessment.Violations.Select(violation => violation.Revision).ShouldContainOnly(1u, 2u);
    [Fact] void should_report_changed_stored_schemas() => _assessment.Violations.All(violation => violation.Kind == "BaselinedRevisionChanged").ShouldBeTrue();
}
#endif
