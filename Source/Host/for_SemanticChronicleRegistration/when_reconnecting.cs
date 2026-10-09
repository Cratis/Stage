// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

using CommandResult = Cratis.Chronicle.Contracts.Commands.CommandResult;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration;

public class when_reconnecting : Specification
{
    IServices _contracts = null!;
    ConnectionLifecycle _lifecycle = null!;

    async Task Establish()
    {
        _contracts = Substitute.For<IServices>();
        _contracts.EventStores.EnsureEventStore(Arg.Any<EnsureEventStoreRequest>()).Returns(CommandResult.Success(Guid.Empty));
        _contracts.EventTypes.RegisterEventTypes(Arg.Any<RegisterEventTypesRequest>()).Returns(CommandResult.Success(Guid.Empty));
        _contracts.Sequences.TailSequenceNumber(Arg.Any<TailSequenceNumberRequest>()).Returns(QueryResult<EventSequenceTailResponse>.Success(Guid.Empty, new() { SequenceNumber = ulong.MaxValue }));
        _lifecycle = new(NullLogger<ConnectionLifecycle>.Instance);
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        connection.Lifecycle.Returns(_lifecycle);
        ((IChronicleServicesAccessor)connection).Services.Returns(_contracts);
        var store = Substitute.For<IEventStore>();
        store.Name.Returns(new EventStoreName("StageSemanticReconnect"));
        store.Connection.Returns(connection);
        var client = Substitute.For<IChronicleClient>();
        client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName?>()).Returns(store);
        await SemanticChronicleRegistration.Register(client, "StageSemanticReconnect", specification_plan.Create());
    }

    async Task Because()
    {
        await _lifecycle.Disconnected();
        await _lifecycle.Connected();
    }

    [Fact] async Task should_ensure_the_store_again() => await _contracts.EventStores.Received(2).EnsureEventStore(Arg.Is<EnsureEventStoreRequest>(request => request.Name == "StageSemanticReconnect"));
    [Fact] async Task should_register_the_semantic_event_contracts_again() => await _contracts.EventTypes.Received(2).RegisterEventTypes(Arg.Is<RegisterEventTypesRequest>(request => request.EventStore == "StageSemanticReconnect" && request.Types.Count() == 1));
    [Fact] async Task should_not_rebuild_the_published_world_on_reconnect() => await _contracts.Sequences.Received(1).TailSequenceNumber(Arg.Any<TailSequenceNumberRequest>());
}
