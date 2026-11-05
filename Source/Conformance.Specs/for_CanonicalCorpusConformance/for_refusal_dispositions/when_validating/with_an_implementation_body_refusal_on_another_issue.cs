// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.for_refusal_dispositions.when_validating;

public class with_an_implementation_body_refusal_on_another_issue : Specification
{
    readonly refusal_fingerprint _fingerprint = new("STAGE-IMPL-003", "command", "sem1:one");
    IReadOnlyList<string> _problems = null!;

    void Because() => _problems = refusal_dispositions.Problems("EsmGoldenVectors.V3", conformance_surface.Host, new("refused", [new(_fingerprint, 316, null)]), new("refused", [_fingerprint], new Dictionary<refusal_fingerprint, string>()));

    [Fact] void should_reject_the_record() => _problems.Any(problem => problem.StartsWith("EsmGoldenVectors.V3 / Host: issue-316-tracks-STAGE-IMPL-003(", StringComparison.Ordinal)).ShouldBeTrue();
}
#endif
