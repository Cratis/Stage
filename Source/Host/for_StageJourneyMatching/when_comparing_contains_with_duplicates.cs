// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_StageJourneyMatching.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageJourneyMatching;

public class when_comparing_contains_with_duplicates : a_comparison
{
    IReadOnlyList<string> _matchedWithExtras = [];

    void Because()
    {
        Compare("""[{ "a": 1 }, { "a": 1 }]""", """[{ "a": 2 }, { "a": 1, "b": 3 }, { "a": 1 }]""");
        _matchedWithExtras = [.. _mismatches.Select(mismatch => mismatch.Path)];
        Compare("""[{ "a": 1 }, { "a": 1 }]""", """[{ "a": 1 }, { "a": 2 }]""");
    }

    [Fact] void should_match_each_expected_element_to_a_distinct_actual_element() => _matchedWithExtras.ShouldBeEmpty();
    [Fact] void should_not_reuse_an_actual_element() => _mismatches.Single().Path.ShouldEqual("$[1]");
    [Fact] void should_report_no_actual_value() => _mismatches.Single().Actual.ShouldBeNull();
}
