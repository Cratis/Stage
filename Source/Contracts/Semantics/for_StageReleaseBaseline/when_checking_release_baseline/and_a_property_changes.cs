// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_StageReleaseBaseline.when_checking_release_baseline;

public class and_a_property_changes : given.a_release_baseline
{
    void Because() => _assessment = StageReleaseBaseline.Check(Changed(Source.Replace("value String", "value Bool", StringComparison.Ordinal)), _baseline);
    [Fact] void should_report_a_changed_revision() => _assessment.Violations.Single().Kind.ShouldEqual("BaselinedRevisionChanged");
    [Fact] void should_preserve_contract_identity() => _assessment.Violations.Single().ContractId.ShouldEqual(EventContractId.Parse(_baseline.Document!.Entries.Single(entry => entry.EventType == "Recorded").Event));
    [Fact] void should_include_the_admitted_release() => _assessment.Violations.Single().AdmittedIn.ShouldEqual("v1.0");
    [Fact] void should_include_the_stored_name() => _assessment.Violations.Single().StoredName.ShouldEqual("Recorded");
    [Fact] void should_include_a_display_hint() => _assessment.Violations.Single().DisplayHint.Parts.ShouldNotBeEmpty();
    [Fact] void should_include_the_first_differing_path() => _assessment.Violations.Single().Path.ShouldEqual("$.properties[0].type.primitive");
}
#endif
