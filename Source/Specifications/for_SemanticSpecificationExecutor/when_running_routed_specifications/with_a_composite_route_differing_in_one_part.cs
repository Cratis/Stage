// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_routed_specifications;

public class with_a_composite_route_differing_in_one_part : a_routed_plan
{
    bool _matchingPassed;

    async Task Because()
    {
        var uuid = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Uuid);
        var stream = _plan.Model.Application.EventSources[0].Streams[0] with { StreamIdType = null, StreamIdParts = [new("project", uuid), new("period", Text)] };
        var command = _command with
        {
            Route = _command.Route! with
            {
                StreamId = null,
                StreamIdParts = [new("project", SemanticExpression.FromValue(SemanticValue.Text("3fa85f64-5717-4562-b3fc-2c963f66afa6"))), new("period", _command.Route.StreamId!)]
            }
        };
        var expected = _specification.ThenEvents[0];
        var route = expected.Route! with
        {
            StreamId = null,
            StreamIdParts = [new("project", SemanticValue.Text("3FA85F64-5717-4562-B3FC-2C963F66AFA6")), new("period", SemanticValue.Text("2026-10"))]
        };
        await Run(With(_specification with { ThenEvents = [expected with { Route = route }] }, command, stream));
        AssertParity(true);
        _matchingPassed = _reference.Passed;
        route = route with { StreamIdParts = [route.StreamIdParts[0], route.StreamIdParts[1] with { Value = SemanticValue.Text("2026-11") }] };
        await Run(With(_specification with { ThenEvents = [expected with { Route = route }] }, command, stream));
    }

    [Fact] void should_compare_canonical_uuid_parts_and_declaration_order() => _matchingPassed.ShouldBeTrue();
    [Fact] void should_fail_for_one_different_part_like_the_reference() => AssertParity(false);
}
