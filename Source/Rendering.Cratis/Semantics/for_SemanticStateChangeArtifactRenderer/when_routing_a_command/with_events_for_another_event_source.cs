// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer.when_routing_a_command;

public class with_events_for_another_event_source : given.a_routed_command
{
    void Establish()
    {
        var other = _command.Properties[0] with { Id = SemanticId.Parse($"sem1:{new string('a', 64)}"), Name = "otherId", IsIdentifier = true };
        var second = _slice.Events[0] with { Id = SemanticId.Parse($"sem1:{new string('b', 64)}"), Name = "OtherDeposited", ContractId = EventContractId.Parse($"evt1:{new string('b', 64)}"), Properties = [_slice.Events[0].Properties[0] with { Id = SemanticId.Parse($"sem1:{new string('c', 64)}") }] };
        _slice = _slice with { Events = [_slice.Events[0], second] };
        _command = _command with
        {
            Properties = [.. _command.Properties, other],
            Produces = [_command.Produces[0], _command.Produces[0] with { EventContract = second.Id, Destination = SemanticExpression.Property(SemanticExpressionRootKind.Command, other.Id), Mappings = [_command.Produces[0].Mappings[0] with { TargetProperty = second.Properties[0].Id }] }]
        };
    }
    void Because() => Plan();

    [Fact] void should_keep_the_other_destination() => _code.Contains("EventForEventSourceId(OtherId, new", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_route_every_production() => (_code.Split("EventStreamId =", StringSplitOptions.None).Length - 1).ShouldEqual(2);
    [Fact] void should_compile() => RenderedOutput.Errors(Files()).ShouldBeEmpty();
}
