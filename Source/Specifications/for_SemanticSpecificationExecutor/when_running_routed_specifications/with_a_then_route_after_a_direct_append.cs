// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_routed_specifications;

public class with_a_then_route_after_a_direct_append : a_routed_plan
{
    async Task Because()
    {
        var expected = _specification.ThenEvents[0];
        await Run(With(_specification with
        {
            When = null,
            WhenAppended = new(expected.EventContract, expected.Values) { EventSource = new(Text, SemanticValue.Text("acc-1")), Route = expected.Route }
        }));
    }

    [Fact] void should_not_treat_the_append_action_as_a_following_fact() => AssertParity(false);
}
