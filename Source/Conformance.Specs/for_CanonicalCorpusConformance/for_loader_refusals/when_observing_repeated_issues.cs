// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.for_loader_refusals;

public class when_observing_repeated_issues : Specification
{
    conformance_observation _observed = null!;

    void Because() => _observed = loader_refusals.Observe([
        ("UnsupportedValidation", "sem1:one", "Zeta rule"),
        ("UnsupportedValidation", "sem1:one", "Alpha rule"),
        ("UnsupportedValidation", "sem1:one", "Alpha rule"),
        ("UnsupportedQuery", "sem1:one", "Query rule"),
        ("UnsupportedValidation", "sem1:two", "Another artifact")]);

    [Fact] void should_keep_one_fingerprint_per_kind_and_artifact() => _observed.Refusals.ShouldContainOnly([new("STAGE-PLAN-004", "UnsupportedQuery", "sem1:one"), new("STAGE-PLAN-004", "UnsupportedValidation", "sem1:one"), new("STAGE-PLAN-004", "UnsupportedValidation", "sem1:two")]);
    [Fact] void should_keep_multiplicity_and_ordered_distinct_details() => _observed.Details[new("STAGE-PLAN-004", "UnsupportedValidation", "sem1:one")].ShouldEqual($"Occurrences: 3{Environment.NewLine}Alpha rule{Environment.NewLine}Zeta rule");
}
#endif
