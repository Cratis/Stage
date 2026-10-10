// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_routed_specifications;

public class with_a_differing_stream_id : a_routed_plan
{
    async Task Because()
    {
        var expected = _specification.ThenEvents[0];
        await Run(With(_specification with { ThenEvents = [expected with { Route = expected.Route! with { StreamId = SemanticValue.Text("2026-11") } }] }));
    }

    [Fact] void should_fail_like_the_reference() => AssertParity(false);
}
