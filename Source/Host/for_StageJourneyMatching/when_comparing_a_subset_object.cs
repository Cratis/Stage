// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_StageJourneyMatching.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageJourneyMatching;

public class when_comparing_a_subset_object : a_comparison
{
    void Because() => Compare("""{ "Name": "Apollo", "owner": { "id": 7 } }""", """{ "id": "a", "name": "Apollo", "owner": { "id": 7.0, "role": "lead" }, "extra": [1] }""");

    [Fact] void should_ignore_extra_properties_and_name_case() => _mismatches.ShouldBeEmpty();
}
