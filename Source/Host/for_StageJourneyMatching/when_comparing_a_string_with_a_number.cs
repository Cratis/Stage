// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_StageJourneyMatching.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageJourneyMatching;

public class when_comparing_a_string_with_a_number : a_comparison
{
    void Because() => Compare("""{ "count": "1", "total": 2 }""", """{ "count": 1, "total": "2" }""");

    [Fact] void should_not_coerce_either_way() => _mismatches.Select(mismatch => mismatch.Path).ShouldContainOnly(["$.count", "$.total"]);
    [Fact] void should_name_both_kinds() => _mismatches[0].Reason.ShouldEqual("expected a string, found a number");
}
