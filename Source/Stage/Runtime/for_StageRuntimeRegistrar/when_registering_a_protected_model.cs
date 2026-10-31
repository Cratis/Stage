// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Stage.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Runtime.for_StageRuntimeRegistrar;

public class when_registering_a_protected_model
{
    [Fact]
    public async Task should_fault_before_resolving_a_client_or_writing_remotely()
    {
        var model = new EventModel(Guid.NewGuid(), "Billing", []) { ProtectedCommandReads = true };
        var services = Substitute.For<IServiceProvider>();

        var refusal = await Assert.ThrowsAsync<InvalidEventModel>(() =>
            StageRuntimeRegistrar.RegisterAsync(services, "event-store", model, NullLogger.Instance));

        Assert.Contains("STAGE-ESM-044:", refusal.Message);
        services.DidNotReceive().GetService(Arg.Any<Type>());
    }

    [Fact]
    public async Task should_still_register_an_unprotected_model()
    {
        var contracts = Substitute.For<IServices>();
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        connection.Lifecycle.Returns(new ConnectionLifecycle(NullLogger<ConnectionLifecycle>.Instance));
        ((IChronicleServicesAccessor)connection).Services.Returns(contracts);
        var store = Substitute.For<IEventStore>();
        store.Name.Returns(new EventStoreName("StageUnprotected"));
        store.Connection.Returns(connection);
        var client = Substitute.For<IChronicleClient>();
        client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName?>()).Returns(store);
        await using var provider = new ServiceCollection().AddSingleton(client).BuildServiceProvider();
        var model = new EventModel(Guid.NewGuid(), "Billing", []);

        await StageRuntimeRegistrar.RegisterAsync(provider, "StageUnprotected", model, NullLogger.Instance);

        await client.Received().GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName?>());
    }
}
#endif
