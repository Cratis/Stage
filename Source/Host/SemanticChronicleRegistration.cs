// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.EventSequences;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Semantics;
using ChronicleEvents = Cratis.Chronicle.Contracts.Events;
using ChronicleEventTypes = Cratis.Chronicle.Contracts.EventTypes;
using ChronicleProjections = Cratis.Chronicle.Contracts.Projections;
using ChronicleReadModels = Cratis.Chronicle.Contracts.ReadModels;
using ChronicleSequences = Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Stage.Host;

internal static class SemanticChronicleRegistration
{
    internal static bool IsEmpty(ulong tail) => tail == ulong.MaxValue;

    internal static async Task<bool> Register(IChronicleClient client, string name, SemanticExecutionPlan plan)
    {
        var store = await client.GetEventStore(name);
        await store.Connection.Connect();
        var accessor = (IChronicleServicesAccessor)store.Connection;
        (await accessor.Services.EventStores.EnsureEventStore(new EnsureEventStoreRequest { Name = store.Name })).EnsureSuccess();
        var tail = (await accessor.Services.Sequences.TailSequenceNumber(new ChronicleSequences.TailSequenceNumberRequest
        {
            EventStore = store.Name,
            Namespace = EventStoreNamespaceName.Default,
            EventSequenceId = EventSequenceId.Log
        })).EnsureSuccess().SequenceNumber;
        if (!IsEmpty(tail))
        {
            return false;
        }

        var registrations = plan.Events.Values.Select(@event =>
        {
            var schema = SemanticSchemas.Schema(@event.Properties, plan.Model.Application);
            return new ChronicleEvents.EventTypeRegistration
            {
                Type = new ChronicleEvents.EventType { Id = @event.Name, Generation = 1 },
                Schema = schema,
                Owner = ChronicleEvents.EventTypeOwner.Client,
                Source = ChronicleEvents.EventTypeSource.Code,
                Generations = [new ChronicleEvents.EventTypeGenerationDefinition { Generation = 1, Schema = schema }]
            };
        }).ToArray();
        if (registrations.Length > 0)
        {
            (await accessor.Services.EventTypes.RegisterEventTypes(new ChronicleEventTypes.RegisterEventTypesRequest
            {
                EventStore = store.Name,
                Types = registrations
            })).EnsureSuccess();
        }

        var mirrors = plan.Projections.Values
            .Select(projection => SemanticProjectionMirrors.TryLower(plan, projection, out var model, out var definition, out _)
                ? (Model: model, Projection: definition)
                : (Model: null, Projection: null))
            .Where(pair => pair.Model is not null && pair.Projection is not null)
            .ToArray();
        if (mirrors.Length > 0)
        {
            await accessor.Services.ReadModels.RegisterMany(new ChronicleReadModels.RegisterManyRequest
            {
                EventStore = store.Name,
                Owner = ChronicleReadModels.ReadModelOwner.Client,
                ReadModels = [.. mirrors.Select(pair => pair.Model!)],
                Source = ChronicleReadModels.ReadModelSource.User
            });
            await accessor.Services.Projections.Register(new ChronicleProjections.RegisterRequest
            {
                EventStore = store.Name,
                Owner = ChronicleProjections.ProjectionOwner.Client,
                Projections = [.. mirrors.Select(pair => pair.Projection!)]
            });
        }

        return true;
    }
}
