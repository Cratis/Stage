// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_StageJourneyMatching.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageJourneyMatching;

public class when_comparing_ordered_elements : a_comparison
{
    int _inOrder;

    void Because()
    {
        Compare("""["a", "c"]""", """["a", "b", "c"]""", StageJourneyListMatch.Ordered);
        _inOrder = _mismatches.Count;
        Compare("""["c", "a"]""", """["a", "b", "c"]""", StageJourneyListMatch.Ordered);
    }

    [Fact] void should_allow_extras_between_matches() => _inOrder.ShouldEqual(0);
    [Fact] void should_refuse_a_reversed_order() => _mismatches.Single().Path.ShouldEqual("$[1]");
}
