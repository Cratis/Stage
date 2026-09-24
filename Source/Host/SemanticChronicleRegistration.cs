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
        if (!IsEmpty(tail))
        {
            EnsureMirrored(plan);
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

        return IsEmpty(tail) ? SemanticWorld.Empty : await Rebuild(accessor, store.Name, plan, tail);
    }

    internal static void EnsureMirrored(SemanticExecutionPlan plan)
    {
        foreach (var projection in plan.Projections.Values)
        {
            if (!SemanticProjectionMirrors.TryLower(plan, projection, out _, out _, out var reason))
            {
                throw new SemanticWorldRebuildRefused($"Projection '{projection.Name}' is not mirrored exactly into Chronicle: {reason}");
            }
        }
    }

    internal static void EnsureNoFailures(IEnumerable<Cratis.Chronicle.Contracts.Observation.FailedPartition> failed, IReadOnlySet<string> mirrorIds)
    {
        if (failed.Any(partition => mirrorIds.Contains(partition.ObserverId) && !partition.IsResolved))
        {
            throw new SemanticWorldRebuildRefused("The Chronicle mirror has failed partitions.");
        }
    }

    static async Task<SemanticWorld> Rebuild(IChronicleServicesAccessor accessor, string name, SemanticExecutionPlan plan, ulong tail)
    {
        var mirrorIds = plan.Projections.Values.Select(projection =>
        {
            SemanticProjectionMirrors.TryLower(plan, projection, out _, out var mirror, out _);
            return mirror!.Identifier;
        }).ToHashSet(StringComparer.Ordinal);
        var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        while (mirrorIds.Count > 0)
        {
            var observers = (await accessor.Services.Observers.GetObservers(new Cratis.Chronicle.Contracts.Observation.AllObserversRequest
            {
                EventStore = name,
                Namespace = EventStoreNamespaceName.Default
            })).Where(observer => mirrorIds.Contains(observer.Id)).ToArray();
            if (observers.Length == mirrorIds.Count && observers.All(observer => observer.LastHandledEventSequenceNumber >= tail))
            {
                break;
            }

            await EnsureNoFailedPartitions(accessor, name, mirrorIds);
            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new SemanticWorldRebuildRefused($"Mirror projections have not reached event-log tail {tail}: {string.Join(", ", observers.Select(observer => $"{observer.Id}={observer.LastHandledEventSequenceNumber}/{observer.IsSubscribed}"))}.");
            }

            await Task.Delay(100);
        }

        await EnsureNoFailedPartitions(accessor, name, mirrorIds);

        var events = (await accessor.Services.Sequences.FromSequenceNumber(new ChronicleSequences.FromSequenceNumberRequest
        {
            EventStore = name,
            Namespace = EventStoreNamespaceName.Default,
            EventSequenceId = EventSequenceId.Log,
            FromEventSequenceNumber = 0
        })).EnsureSuccess().ToArray();
        var instances = new Dictionary<SemanticId, IReadOnlyList<string>>();
        foreach (var readModel in plan.ReadModels.Values.Where(model => plan.Projections.Values.Any(projection => projection.ReadModel == model.Id)))
        {
            var mirror = plan.Projections.Values.Single(projection => projection.ReadModel == readModel.Id);
            SemanticProjectionMirrors.TryLower(plan, mirror, out var definition, out _, out _);
            var identifier = definition!.Type.Identifier;
            var rows = new List<string>();
            long? total = null;
            for (var page = 0; ; page++)
            {
                var result = await accessor.Services.ReadModels.GetInstances(new ChronicleReadModels.GetInstancesRequest
                {
                    EventStore = name,
                    Namespace = EventStoreNamespaceName.Default,
                    ReadModel = identifier,
                    Page = page,
                    PageSize = 100
                });
                if (result.TotalCount < 0 || (total is not null && total != result.TotalCount) ||
                    result.Instances.Count > 100 || rows.Count + result.Instances.Count > result.TotalCount ||
                    (result.Instances.Count == 0 && rows.Count < result.TotalCount))
                {
                    throw new SemanticWorldRebuildRefused($"Chronicle mirror for '{readModel.Name}' returned an incomplete or inconsistent page.");
                }

                total = result.TotalCount;
                rows.AddRange(result.Instances);
                if (rows.Count == total)
                {
                    break;
                }
            }

            instances.Add(readModel.Id, rows);
        }

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

        return SemanticWorldRebuilder.Create(plan, events, instances, tail);
    }

    static async Task EnsureNoFailedPartitions(IChronicleServicesAccessor accessor, string name, HashSet<string> mirrorIds)
    {
        var failed = await accessor.Services.FailedPartitions.GetFailedPartitions(new Cratis.Chronicle.Contracts.Observation.GetFailedPartitionsRequest
        {
            EventStore = name,
            Namespace = EventStoreNamespaceName.Default
        });
        EnsureNoFailures(failed, mirrorIds);
    }
}
