// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.for_refusal_dispositions.when_validating;

public class with_a_reserved_name_on_another_vector : Specification
{
    readonly refusal_fingerprint _fingerprint = new("STAGE-ESM-030", null, "sem1:000000000000000000000000000000000000000000000000000000000000232c");
    IReadOnlyList<string> _problems = null!;

    void Because() => _problems = refusal_dispositions.Problems("EsmGoldenVectors.V25", conformance_surface.Renderer, new("refused", [new(_fingerprint, null, "reason")]), new("refused", [_fingerprint], new Dictionary<refusal_fingerprint, string>()));

    [Fact] void should_reject_the_exemption() => _problems.Any(problem => problem.StartsWith("EsmGoldenVectors.V25 / Renderer: unauthorized-by-design(", StringComparison.Ordinal)).ShouldBeTrue();
}
#endif
