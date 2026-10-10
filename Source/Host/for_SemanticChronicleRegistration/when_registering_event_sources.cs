// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.EventSources;
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

public class when_registering_event_sources : Specification
{
    IServices _contracts = null!;
    ConnectionLifecycle _lifecycle = null!;
    readonly List<RegisterEventSourcesRequest> _requests = [];

    async Task Establish()
    {
        _contracts = Substitute.For<IServices>();
        _contracts.EventStores.EnsureEventStore(Arg.Any<EnsureEventStoreRequest>()).Returns(CommandResult.Success(Guid.Empty));
        _contracts.EventTypes.RegisterEventTypes(Arg.Any<RegisterEventTypesRequest>()).Returns(CommandResult.Success(Guid.Empty));
        _contracts.EventSources.RegisterEventSources(Arg.Do<RegisterEventSourcesRequest>(_requests.Add)).Returns(CommandResult.Success(Guid.Empty));
        _contracts.Sequences.TailSequenceNumber(Arg.Any<TailSequenceNumberRequest>()).Returns(QueryResult<EventSequenceTailResponse>.Success(Guid.Empty, new() { SequenceNumber = ulong.MaxValue }));
        _lifecycle = new(NullLogger<ConnectionLifecycle>.Instance);
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        connection.Lifecycle.Returns(_lifecycle);
        ((IChronicleServicesAccessor)connection).Services.Returns(_contracts);
        var store = Substitute.For<IEventStore>();
        store.Name.Returns(new EventStoreName("Routed"));
        store.Connection.Returns(connection);
        var client = Substitute.For<IChronicleClient>();
        client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName?>()).Returns(store);
        var plan = compiled_plan.From("""
            eventsource Account
              id "stored-account"
              stream Transactions
                id "stored-transactions"
              stream Statements
            module Banking
              feature Deposits
                slice StateChange Deposit
                  event Deposited
                    amount Int
            """);
        await SemanticChronicleRegistration.Register(client, "Routed", plan);
    }

    async Task Because()
    {
        await _lifecycle.Disconnected();
        await _lifecycle.Connected();
    }

    [Fact] void should_register_on_initial_connect_and_reconnect() => _requests.Count.ShouldEqual(2);
    [Fact] void should_target_the_event_store() => _requests.TrueForAll(request => request.EventStore == "Routed").ShouldBeTrue();
    [Fact] void should_use_the_stored_source_name() => _requests[0].Sources.Single().Name.ShouldEqual("stored-account");
    [Fact] void should_use_the_stored_stream_names() => _requests[0].Sources.Single().Streams.Select(stream => stream.Name).ShouldContainOnly(["stored-transactions", "Statements"]);
    [Fact] void should_register_client_owned_sources() => _requests[0].Sources.Single().Owner.ShouldEqual(EventSourceOwner.Client);
    [Fact] void should_set_no_source_concurrency_dimensions() => _requests[0].Sources.Single().Concurrency.ShouldEqual(ConcurrencyDimensions.None);
    [Fact] void should_set_no_stream_concurrency_dimensions() => _requests[0].Sources.Single().Streams.All(stream => stream.Concurrency == ConcurrencyDimensions.None).ShouldBeTrue();
}
