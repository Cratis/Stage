// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.for_refusal_dispositions.when_validating;

public class with_each_refusal_recorded_once_by_distinct_issues : Specification
{
    readonly refusal_fingerprint _fingerprint = new("STAGE-ESM-010", null, "sem1:one");
    readonly refusal_fingerprint _other = new("STAGE-ESM-020", null, "sem1:two");
    IReadOnlyList<string> _problems = null!;

    void Because() => _problems = refusal_dispositions.Problems("GenerationFixturesCorpus.V21", conformance_surface.Renderer, new("refused", [new(_fingerprint, 501, null), new(_other, 513, null)]), new("refused", [_fingerprint, _other], new Dictionary<refusal_fingerprint, string>()));

    [Fact] void should_report_no_problems() => _problems.ShouldBeEmpty();
}
#endif
