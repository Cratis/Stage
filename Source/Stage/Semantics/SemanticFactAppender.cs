// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.EventSequences;
using Cratis.DependencyInjection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Runtime;
using ChronicleSequences = Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Appends one accepted group of semantic facts atomically.
/// </summary>
public interface IAppendSemanticFacts
{
    /// <summary>
    /// Appends the facts with their command occurrence.
    /// </summary>
    /// <param name="facts">The accepted facts.</param>
    /// <param name="occurrence">The shared occurrence metadata.</param>
    /// <param name="expectedTail">The event-log tail captured before evaluation (ulong.MaxValue for an empty log).</param>
    /// <returns>The append operation.</returns>
    Task Append(IReadOnlyList<SemanticFact> facts, SemanticCommandOccurrence occurrence, ulong expectedTail);
}

/// <summary>
/// Reads Chronicle's event-log tail to distinguish a rejected append from an indeterminate one.
/// </summary>
public interface ISemanticFactTail
{
    /// <summary>
    /// Reads the current Chronicle event-log tail before or after an append.
    /// </summary>
    /// <returns>The sequence number, including the empty-log sentinel.</returns>
    Task<ulong> Tail();
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "This exception is only an internal outcome marker between the fact appender and runtime.")]
internal sealed class SemanticFactTailChanged(ulong actualTail) : Exception("The event-log tail changed outside this session.")
{
    public ulong ActualTail { get; } = actualTail;
}

[IgnoreConvention]
internal sealed class SemanticFactAppender(IChronicleClient client, StageEventStoreName eventStore, SemanticExecutionPlan plan) : IAppendSemanticFacts, ISemanticFactTail
{
    // The key is only a label when EventSourceId is false; even an identically named fact source does not narrow the lookup.
    const string TailScopeLabel = "__stage:event-log-tail__";

    public async Task<ulong> Tail()
    {
        var store = await client.GetEventStore(eventStore.Value);
        await store.Connection.Connect();
        var accessor = (IChronicleServicesAccessor)store.Connection;
        var response = await accessor.Services.Sequences.TailSequenceNumber(new ChronicleSequences.TailSequenceNumberRequest
        {
            EventStore = store.Name,
            Namespace = EventStoreNamespaceName.Default,
            EventSequenceId = EventSequenceId.Log
        });
        return response.EnsureSuccess().SequenceNumber;
    }

    public async Task Append(IReadOnlyList<SemanticFact> facts, SemanticCommandOccurrence occurrence, ulong expectedTail)
    {
        if (facts.Count == 0)
        {
            return;
        }

        var store = await client.GetEventStore(eventStore.Value);
        await store.Connection.Connect();
        var accessor = (IChronicleServicesAccessor)store.Connection;
        var response = await accessor.Services.Sequences.AppendManyForEventSources(new ChronicleSequences.AppendManyForEventSourcesRequest
        {
            EventStore = store.Name,
            Namespace = EventStoreNamespaceName.Default,
            EventSequenceId = EventSequenceId.Log,
            ConcurrencyScopes =
            [
                new ChronicleSequences.EventSourceConcurrencyScope
                {
                    EventSourceId = TailScopeLabel,
                    Scope = new ChronicleSequences.ConcurrencyScope
                    {
                        EventSourceId = false,
                        EventTypes = [],
                        SequenceNumber = expectedTail,
                        ExpectsNoMatchingEvent = expectedTail == ulong.MaxValue
                    }
                }
            ],
            CausedBy = new ChronicleSequences.Identity
            {
                Subject = occurrence.Subject,
                Name = occurrence.Name,
                UserName = occurrence.UserName
            },
            Events = [.. facts.Select(fact => new ChronicleSequences.EventForEventSourceId
            {
                EventSourceId = SemanticJsonValues.ToObject(fact.Destination)?.ToString() ?? string.Empty,
                EventType = new ChronicleSequences.EventType { Id = plan.Events[fact.EventContract].Name, Generation = 1 },
                Content = JsonSerializer.Serialize(fact.Values.ToDictionary(
                    entry => plan.Events[fact.EventContract].Properties.Single(property => property.Id == entry.TargetProperty).Name,
                    entry =>
                    {
                        var property = plan.Events[fact.EventContract].Properties.Single(candidate => candidate.Id == entry.TargetProperty);
                        return SemanticJsonValues.ToObject(entry.Value, property.Type, plan.Model.Application);
                    })),
                Tags = fact.Tags,
                Occurred = (Cratis.Chronicle.Contracts.Primitives.SerializableDateTimeOffset?)occurrence.Occurred ?? new(),
            })]
        });
        response.EnsureSuccess();
        var violation = response.Response.ConcurrencyViolations.FirstOrDefault();
        if (violation is not null)
        {
            // Chronicle rejected the entire batch before appending anything; the outcome is known.
            throw new SemanticFactTailChanged(violation.ActualSequenceNumber);
        }

        var rejection = ProducedEventAppender.Rejection(response.Response);
        if (rejection is not null)
        {
            throw ProducedEventAppender.ExceptionFor(rejection);
        }
    }
}
