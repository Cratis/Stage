// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_StageJourneyMatching.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageJourneyMatching;

public class when_comparing_null_and_absent_values : a_comparison
{
    int _nullAgainstAbsent;
    int _nullAgainstNull;
    IReadOnlyList<string> _reasons = [];

    void Because()
    {
        Compare("""{ "closed": null }""", "{}");
        _nullAgainstAbsent = _mismatches.Count;
        Compare("""{ "closed": null }""", """{ "closed": null }""");
        _nullAgainstNull = _mismatches.Count;
        Compare("""{ "closed": null, "name": "a", "owner": "b" }""", """{ "closed": true, "owner": null }""");
        _reasons = [.. _mismatches.Select(mismatch => $"{mismatch.Path} {mismatch.Reason}")];
    }

    [Fact] void should_match_an_expected_null_with_an_absent_value() => _nullAgainstAbsent.ShouldEqual(0);
    [Fact] void should_match_an_expected_null_with_null() => _nullAgainstNull.ShouldEqual(0);
    [Fact] void should_tell_null_missing_and_present_apart() => _reasons.ShouldContainOnly(["$.closed expected null", "$.name missing", "$.owner expected a value, found null"]);
}
