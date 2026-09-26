// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.EventSequences;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Semantics;
using ChronicleEvents = Cratis.Chronicle.Contracts.Events;
using ChronicleEventTypes = Cratis.Chronicle.Contracts.EventTypes;
using ChronicleSequences = Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Stage.Host;

internal static class SemanticChronicleRegistration
{
    internal static bool IsEmpty(ulong tail) => tail == ulong.MaxValue;

    internal static async Task<SemanticWorld> Register(IChronicleClient client, string name, SemanticExecutionPlan plan)
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

        return IsEmpty(tail) ? SemanticWorld.Empty : await Rebuild(accessor, store.Name, plan, tail);
    }

    internal static async Task<SemanticWorld> Rebuild(IChronicleServicesAccessor accessor, string name, SemanticExecutionPlan plan, ulong tail)
    {
        var events = (await accessor.Services.Sequences.FromSequenceNumber(new ChronicleSequences.FromSequenceNumberRequest
        {
            EventStore = name,
            Namespace = EventStoreNamespaceName.Default,
            EventSequenceId = EventSequenceId.Log,
            FromEventSequenceNumber = 0
        })).EnsureSuccess().ToArray();
        var after = (await accessor.Services.Sequences.TailSequenceNumber(new ChronicleSequences.TailSequenceNumberRequest
        {
            EventStore = name,
            Namespace = EventStoreNamespaceName.Default,
            EventSequenceId = EventSequenceId.Log
        })).EnsureSuccess().SequenceNumber;
        if (after != tail)
        {
            throw new SemanticWorldRebuildRefused("The Chronicle event-log tail changed during world reconstruction.");
        }

        return SemanticWorldRebuilder.Create(plan, events, tail);
    }
}
