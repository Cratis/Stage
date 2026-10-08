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
using ChronicleProjections = Cratis.Chronicle.Contracts.Projections;
using ChronicleReadModels = Cratis.Chronicle.Contracts.ReadModels;
using ChronicleSequences = Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Stage.Host;

internal static class SemanticChronicleRegistration
{
    internal static bool IsEmpty(ulong tail) => tail == ulong.MaxValue;

    internal static async Task<SemanticWorld> Register(IChronicleClient client, string name, SemanticExecutionPlan plan, List<StageUnsupportedIssue> mirrorIssues)
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

        // Mirrors only expose read models in the Workbench. The semantic world is established from
        // event history, never from mirror snapshots, observer progress, or registration outcomes.
        var world = IsEmpty(tail) ? SemanticWorld.Empty : await Rebuild(accessor, store.Name, plan, tail);
        mirrorIssues.AddRange(await RegisterMirrors(accessor, store.Name, plan));

        return world;
    }

    internal static async Task<IReadOnlyList<StageUnsupportedIssue>> RegisterMirrors(IChronicleServicesAccessor accessor, string name, SemanticExecutionPlan plan)
    {
        var issues = new List<StageUnsupportedIssue>();
        var mirrors = new List<(string Artifact, ChronicleReadModels.ReadModelDefinition Model, ChronicleProjections.ProjectionDefinition Projection)>();
        foreach (var projection in plan.Projections.Values)
        {
            if (SemanticProjectionMirrors.TryLower(plan, projection, out var model, out var definition, out var reason))
            {
                mirrors.Add((projection.Id.ToString(), model!, definition!));
            }
            else
            {
                issues.Add(new("ProjectionMirror", projection.Id.ToString(), $"Projection '{projection.Name}' is not visible in the Workbench: {reason}"));
            }
        }

        if (mirrors.Count > 0)
        {
            try
            {
                await accessor.Services.ReadModels.RegisterMany(new ChronicleReadModels.RegisterManyRequest
                {
                    EventStore = name,
                    Owner = ChronicleReadModels.ReadModelOwner.Client,
                    ReadModels = [.. mirrors.Select(mirror => mirror.Model)],
                    Source = ChronicleReadModels.ReadModelSource.User
                });
                await accessor.Services.Projections.Register(new ChronicleProjections.RegisterRequest
                {
                    EventStore = name,
                    Owner = ChronicleProjections.ProjectionOwner.Client,
                    Projections = [.. mirrors.Select(mirror => mirror.Projection)]
                });
            }
            catch (Exception exception)
            {
                // A failed or unknown mirror publication is reported, not retried or treated as success.
                // It cannot invalidate the independently established in-memory world.
                issues.AddRange(mirrors.Select(mirror => new StageUnsupportedIssue(
                    "ProjectionMirror", mirror.Artifact, $"Workbench mirror registration failed: {exception.Message}")));
            }
        }

        return issues;
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
