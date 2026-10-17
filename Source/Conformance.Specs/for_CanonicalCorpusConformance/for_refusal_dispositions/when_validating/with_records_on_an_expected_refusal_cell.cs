// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.for_refusal_dispositions.when_validating;

public class with_records_on_an_expected_refusal_cell : Specification
{
    readonly refusal_fingerprint _fingerprint = new("STAGE-ESM-010", null, "sem1:one");
    IReadOnlyList<string> _problems = null!;

    void Because() => _problems = refusal_dispositions.Problems("GenerationFixturesCorpus.V21", conformance_surface.Renderer, new("expected-refusal", [new(_fingerprint, 501, null)]), conformance_observation.WithoutRefusals("expected-refusal"));

    [Fact] void should_reject_the_records() => _problems.Any(problem => problem.StartsWith("GenerationFixturesCorpus.V21 / Renderer: records-on-unrefused-cell(", StringComparison.Ordinal)).ShouldBeTrue();
}
#endif
