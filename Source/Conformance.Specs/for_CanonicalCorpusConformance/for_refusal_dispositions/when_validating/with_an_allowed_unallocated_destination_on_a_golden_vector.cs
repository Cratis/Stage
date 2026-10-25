// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.for_refusal_dispositions.when_validating;

public class with_an_allowed_unallocated_destination_on_a_golden_vector : Specification
{
    readonly refusal_fingerprint _fingerprint = new("STAGE-ESM-053", null, "sem1:000000000000000000000000000000000000000000000000000000000000277e");
    IReadOnlyList<string> _problems = null!;

    void Because() => _problems = refusal_dispositions.Problems("EsmGoldenVectors.V11", conformance_surface.Renderer, new("refused", [new(_fingerprint, null, "Stage does not allocate event-source identities for a destination-less production")]), new("refused", [_fingerprint], new Dictionary<refusal_fingerprint, string>()));

    [Fact] void should_report_no_problems() => _problems.ShouldBeEmpty();
}
#endif
