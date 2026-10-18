// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.for_refusal_dispositions.when_validating;

public class with_an_allowed_reserved_name_on_a_golden_vector : Specification
{
    readonly refusal_fingerprint _fingerprint = new("STAGE-ESM-030", null, "sem1:000000000000000000000000000000000000000000000000000000000000232c");
    IReadOnlyList<string> _problems = null!;

    void Because() => _problems = refusal_dispositions.Problems("EsmGoldenVectors.V8", conformance_surface.Renderer, new("refused", [new(_fingerprint, null, "Chronicle reserves the stored name in this frozen golden vector")]), new("refused", [_fingerprint], new Dictionary<refusal_fingerprint, string>()));

    [Fact] void should_report_no_problems() => _problems.ShouldBeEmpty();
}
#endif
