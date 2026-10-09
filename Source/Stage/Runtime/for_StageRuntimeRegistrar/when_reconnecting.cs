// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Runtime.for_StageRuntimeRegistrar;

public class when_reconnecting : Specification
{
    IServices _contracts = null!;
    ConnectionLifecycle _lifecycle = null!;

    async Task Establish()
    {
        _contracts = Substitute.For<IServices>();
        _lifecycle = new(NullLogger<ConnectionLifecycle>.Instance);
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        connection.Lifecycle.Returns(_lifecycle);
        ((IChronicleServicesAccessor)connection).Services.Returns(_contracts);
        var store = Substitute.For<IEventStore>();
        store.Name.Returns(new EventStoreName("StageReconnect"));
        store.Connection.Returns(connection);
        var client = Substitute.For<IChronicleClient>();
        client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName?>()).Returns(store);
        await using var provider = new ServiceCollection().AddSingleton(client).BuildServiceProvider();
        var model = EventModelLoader.LoadFromSource("""
            module Catalog
              feature Items
                slice StateChange RegisterItem
                  event ItemRegistered
                    name String
                slice StateView Summary
                  readmodel SummaryModel
                    name String
                  projection Summary => SummaryModel
                    from ItemRegistered key $eventSourceId
                      name = name
            """);
        await StageRuntimeRegistrar.RegisterAsync(provider, "StageReconnect", model, NullLogger.Instance);
    }

    async Task Because()
    {
        await _lifecycle.Disconnected();
        await _lifecycle.Connected();
    }

    [Fact] async Task should_ensure_the_store_on_each_connection() => await _contracts.EventStores.Received(2).EnsureEventStore(Arg.Is<EnsureEventStoreRequest>(request => request.Name == "StageReconnect"));
    [Fact] async Task should_register_event_types_again() => await _contracts.EventTypes.Received(2).RegisterEventTypes(Arg.Is<RegisterEventTypesRequest>(request => request.EventStore == "StageReconnect" && request.Types.Count() == 1));
    [Fact] async Task should_register_read_models_again() => await _contracts.ReadModels.Received(2).RegisterMany(Arg.Is<RegisterManyRequest>(request => request.EventStore == "StageReconnect" && request.ReadModels.Count == 1));
    [Fact] async Task should_register_projections_without_retiring_other_client_projections() => await _contracts.Projections.Received(2).Register(Arg.Is<RegisterRequest>(request => request.EventStore == "StageReconnect" && request.Projections.Count == 1 && !request.FullSet));
}
#endif
