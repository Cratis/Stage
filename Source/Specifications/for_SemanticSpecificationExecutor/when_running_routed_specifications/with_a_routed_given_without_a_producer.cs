// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_routed_specifications;

public class with_a_routed_given_without_a_producer : a_routed_plan
{
    async Task Because()
    {
        var historical = _plan.Events.Values.Single(value => value.Name == "Historical");
        var given = new SemanticSpecificationEvent(historical.Id, [])
        {
            EventSource = new(Text, SemanticValue.Text("history")),
            Route = _specification.ThenEvents[0].Route! with { StreamId = SemanticValue.Text("previous") }
        };
        await Run(With(_specification with { GivenEvents = [given] }));
    }

    [Fact] void should_pass_like_the_reference() => AssertParity(true);
    [Fact] void should_keep_the_historical_route_in_the_reference_world() => _reference.Execution.World.Facts[0].Route!.StreamId.ShouldEqual("previous");
}
