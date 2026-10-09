// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections;
using Cratis.Stage.Contracts;
using Cratis.Stage.Runtime;
using Microsoft.Extensions.Options;
using Xunit;

using ChronicleReadModels = Cratis.Chronicle.Contracts.ReadModels;
using ChronicleSequences = Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Stage.Host.for_CratisServiceConfiguration;

public class when_reconnecting_against_the_pinned_kernel
{
    [Fact]
    public async Task should_keep_stage_projections_updating_when_configured()
    {
        var address = Environment.GetEnvironmentVariable("STAGE_CHRONICLE_MONGO_CONNECTION");
        if (string.IsNullOrWhiteSpace(address))
        {
            return;
        }

        var name = $"StageReconnect{Guid.NewGuid():N}";
        var builder = WebApplication.CreateBuilder();
        builder.AddStageCratis(name, "Stage reconnect probe");
        builder.Services.PostConfigure<ChronicleOptions>(options => options.ConnectionString = address);
        await using var app = builder.Build();
        var options = app.Services.GetRequiredService<IOptions<ChronicleOptions>>().Value;
        Assert.False(options.AutoDiscoverAndRegister);
        var client = app.Services.GetRequiredService<IChronicleClient>();
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
        await StageRuntimeRegistrar.RegisterAsync(app.Services, name, model, app.Logger);
        var store = await client.GetEventStore(name);
        var definitions = StageChronicleDefinitions.Build(model, EventSequenceId.Log);
        var readModel = Assert.Single(definitions.ReadModels);
        var projection = Assert.Single(definitions.Projections);
        var handler = new ProjectionHandler(store, projection.Identifier, typeof(object), readModel.ContainerName, EventSequenceId.Log);
        await handler.WaitTillSubscribed(TimeSpan.FromSeconds(60));
        await AppendAndAssert("Before reconnect");

        var connectionId = store.Connection.Lifecycle.ConnectionId;

        // Invalidate the lifecycle through its public API. Connect must establish a new transport and
        // execute the real OnConnected registrations, not merely raise a substitute's event.
        await store.Connection.Lifecycle.Disconnected();
        await store.Connection.Connect();
        Assert.NotEqual(connectionId, store.Connection.Lifecycle.ConnectionId);
        Assert.True(store.Connection.Lifecycle.IsConnected);
        await handler.WaitTillSubscribed(TimeSpan.FromSeconds(60));
        await AppendAndAssert("After reconnect");

        async Task AppendAndAssert(string value)
        {
            var contracts = ((IChronicleServicesAccessor)store.Connection).Services;
            var append = (await contracts.Sequences.AppendMany(new ChronicleSequences.AppendManyRequest
            {
                EventStore = store.Name,
                Namespace = store.Namespace,
                EventSequenceId = EventSequenceId.Log,
                EventSourceId = "item",
                CausedBy = new() { Subject = "stage-probe", Name = "Stage probe", UserName = "stage-probe" },
                Events = [new() { EventType = new() { Id = "ItemRegistered", Generation = 1 }, Content = JsonSerializer.Serialize(new { name = value }) }]
            })).EnsureSuccess();
            Assert.True(append.IsSuccess, string.Join("; ", append.Errors));
            await handler.WaitTillReachesEventSequenceNumber(Assert.Single(append.SequenceNumbers), TimeSpan.FromSeconds(60));
            Assert.Empty(await handler.GetFailedPartitions());
            var instances = await contracts.ReadModels.GetInstances(new ChronicleReadModels.GetInstancesRequest
            {
                EventStore = store.Name,
                Namespace = store.Namespace,
                ReadModel = readModel.Type.Identifier,
                Page = 0,
                PageSize = 10
            });
            using var document = JsonDocument.Parse(Assert.Single(instances.Instances));
            var instance = document.RootElement;
            Assert.Equal(value, instance.GetProperty("name").GetString());
        }
    }
}
