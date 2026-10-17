// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.for_refusal_dispositions.when_validating;

public class with_a_refusal_without_an_artifact : Specification
{
    readonly refusal_fingerprint _fingerprint = new("STAGE-ESM-010", null, "sem1:one");
    IReadOnlyList<string> _problems = null!;

    void Because() => _problems = refusal_dispositions.Problems("GenerationFixturesCorpus.V21", conformance_surface.Renderer, new("refused", []), new("refused", [_fingerprint with { Artifact = "none" }], new Dictionary<refusal_fingerprint, string>()));

    [Fact] void should_reject_the_refusal() => _problems.Any(problem => problem.StartsWith("GenerationFixturesCorpus.V21 / Renderer: refusal-without-artifact(", StringComparison.Ordinal)).ShouldBeTrue();
}
#endif
