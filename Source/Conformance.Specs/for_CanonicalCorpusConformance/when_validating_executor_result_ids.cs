// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance;

public class when_validating_executor_result_ids : Specification
{
    IReadOnlyList<string> _unknown = null!;
    IReadOnlyList<string> _duplicate = null!;
    IReadOnlyList<string> _missing = null!;
    IReadOnlyList<string> _valid = null!;

    void Because()
    {
        _unknown = executor_result_ids.Problems(["a", "b"], ["a", "nonexistent"]);
        _duplicate = executor_result_ids.Problems(["a", "b"], ["a", "a"]);
        _missing = executor_result_ids.Problems(["a", "b"], ["a"]);
        _valid = executor_result_ids.Problems(["a", "b"], ["b", "a"]);
    }

    [Fact] void should_reject_a_same_count_unknown_id() => _unknown.ShouldContain("unknown-result-id(nonexistent)");
    [Fact] void should_reject_duplicate_result_ids() => _duplicate.ShouldContain("duplicate-result-id(a)");
    [Fact] void should_reject_missing_result_ids() => _missing.ShouldContain("missing-result-id(b)");
    [Fact] void should_accept_the_exact_id_set_in_any_order() => _valid.ShouldBeEmpty();
}
#endif
