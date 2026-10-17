// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.for_refusal_dispositions.when_validating;

public class with_the_allowed_invalid_generation_fixture : Specification
{
    IReadOnlyList<string> _problems = null!;

    void Because() => _problems = refusal_dispositions.Problems("GenerationFixturesCorpus.V21", conformance_surface.Renderer, new("refused", [new(refusal_dispositions.InvalidGeneration, null, "Deliberately invalid TenantOrderId generation fixture")]), new("refused", [refusal_dispositions.InvalidGeneration], new Dictionary<refusal_fingerprint, string>()));

    [Fact] void should_report_no_problems() => _problems.ShouldBeEmpty();
}
#endif
