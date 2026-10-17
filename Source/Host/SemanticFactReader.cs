// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Chronicle.EventSequences;
using Cratis.DependencyInjection;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Runtime;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Host;

[IgnoreConvention]
internal sealed class SemanticFactReader(IChronicleClient client, StageEventStoreName eventStore, SemanticExecutionPlan plan) : ISemanticFactReader
{
    public async Task<ImmutableArray<SemanticFact>> Read(ulong from, ulong through)
    {
        var store = await client.GetEventStore(eventStore.Value);
        await store.Connection.Connect();
        var accessor = (IChronicleServicesAccessor)store.Connection;
        var response = await accessor.Services.Sequences.FromSequenceNumber(new FromSequenceNumberRequest
        {
            EventStore = store.Name,
            Namespace = EventStoreNamespaceName.Default,
            EventSequenceId = EventSequenceId.Log,
            FromEventSequenceNumber = from
        });

        // A concurrent append beyond the captured tail belongs to the next guarded attempt, not this world.
        var events = response.EnsureSuccess().Where(@event => @event.Context.SequenceNumber <= through).ToArray();
        if ((ulong)events.Length != through - from + 1 ||
            events.Where((@event, index) => @event.Context.SequenceNumber != from + (ulong)index).Any())
        {
            throw new SemanticWorldRebuildRefused($"The event log cannot be read completely from {from} through {through}.");
        }

        return [.. events.Select(@event => SemanticWorldRebuilder.Fact(plan, @event))];
    }
}
