// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.for_refusal_dispositions.when_validating;

public class with_an_allowed_artifact_on_another_surface : Specification
{
    IReadOnlyList<string> _problems = null!;

    void Because() => _problems = refusal_dispositions.Problems("GenerationFixturesCorpus.V21", conformance_surface.Host, new("refused", [new(refusal_dispositions.InvalidGeneration, null, "reason")]), new("refused", [refusal_dispositions.InvalidGeneration], new Dictionary<refusal_fingerprint, string>()));

    [Fact] void should_reject_the_exemption() => _problems.Any(problem => problem.StartsWith("GenerationFixturesCorpus.V21 / Host: unauthorized-by-design(", StringComparison.Ordinal)).ShouldBeTrue();
}
#endif
