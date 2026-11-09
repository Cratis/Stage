// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_StageJourneyMatching.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageJourneyMatching;

public class when_comparing_empty_expectations : a_comparison
{
    int _emptyObject;

    void Because()
    {
        Compare("{}", """{ "id": 1 }""");
        _emptyObject = _mismatches.Count;
        Compare("[]", "[1, 2]");
    }

    [Fact] void should_match_any_object() => _emptyObject.ShouldEqual(0);
    [Fact] void should_match_any_array() => _mismatches.ShouldBeEmpty();
}
