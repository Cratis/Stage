// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.for_refusal_dispositions.when_validating;

public class with_a_record_for_another_artifact : Specification
{
    readonly refusal_fingerprint _fingerprint = new("STAGE-ESM-010", null, "sem1:one");
    IReadOnlyList<string> _problems = null!;

    void Because() => _problems = refusal_dispositions.Problems("GenerationFixturesCorpus.V21", conformance_surface.Renderer, new("refused", [new(_fingerprint with { Artifact = "sem1:other" }, 501, null)]), new("refused", [_fingerprint], new Dictionary<refusal_fingerprint, string>()));

    [Fact] void should_reject_the_refusal_as_unrecorded() => _problems.Any(problem => problem.StartsWith("GenerationFixturesCorpus.V21 / Renderer: unrecorded-refusal(", StringComparison.Ordinal)).ShouldBeTrue();
}
#endif
