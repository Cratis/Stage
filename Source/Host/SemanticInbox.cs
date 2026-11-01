// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Semantics;

using ContractEventType = Cratis.Chronicle.Contracts.Events.EventType;

namespace Cratis.Stage.Host;

/// <summary>
/// Subscribes an application's event store to the outboxes of the stores its foreign events come from, and hands what arrives in its inboxes to the runtime.
/// </summary>
internal static class SemanticInbox
{
    internal const string SubscriptionPrefix = "stage-inbox:";

    /// <summary>
    /// Gets the stores foreign events come from, with the stored names of the events each one publishes to this application.
    /// </summary>
    /// <param name="plan">The plan declaring the events.</param>
    /// <returns>The foreign event names by origin store.</returns>
    internal static IReadOnlyDictionary<string, string[]> Origins(SemanticExecutionPlan plan) => plan.Events.Values
        .Where(SemanticPublicEvents.IsForeign)
        .GroupBy(contract => contract.Origin!, StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.Select(SemanticStoredEventNames.Of).Distinct(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);

    /// <summary>
    /// Subscribes the store to every origin's outbox and registers the reactors that deliver each inbox to the runtime.
    /// </summary>
    /// <param name="client">The Chronicle client.</param>
    /// <param name="eventStore">The application's event store.</param>
    /// <param name="plan">The plan declaring the foreign events.</param>
    /// <param name="runtime">The runtime that settles what arrives.</param>
    /// <returns>A task that completes once the subscriptions and reactors are in place.</returns>
    internal static async Task Start(IChronicleClient client, string eventStore, SemanticExecutionPlan plan, ISemanticForeignFacts runtime)
    {
        var origins = Origins(plan);
        if (origins.Count == 0)
        {
            return;
        }

        var store = await client.GetEventStore(eventStore);
        await store.Connection.Connect();
        var accessor = (IChronicleServicesAccessor)store.Connection;
        await accessor.Services.EventStoreSubscriptions.Add(new AddEventStoreSubscriptions
        {
            TargetEventStore = store.Name,
            Subscriptions = [.. origins.Select(origin => new EventStoreSubscriptionDefinition
            {
                Identifier = SubscriptionPrefix + origin.Key,
                SourceEventStore = origin.Key,
                EventTypes = [.. origin.Value.Select(name => new ContractEventType { Id = name, Generation = 1 })]
            })]
        });

        foreach (var origin in origins)
        {
            await store.Reactors.Register(
                new ReactorId(SubscriptionPrefix + origin.Key),
                builder =>
                {
                    builder.OnEventSequence(EventSequenceId.InboxPrefix + origin.Key);
                    foreach (var name in origin.Value)
                    {
                        builder.WithEventType(new Cratis.Chronicle.Events.EventType(name, 1));
                    }
                },
                (received, _) => Deliver(plan, runtime, origin.Key, received));
        }
    }

    internal static Task Deliver(SemanticExecutionPlan plan, ISemanticForeignFacts runtime, string origin, ReactorEvent received)
    {
        var fact = Fact(plan, origin, received);

        return runtime.Receive(fact, new(received.Context.Occurred, string.Empty, origin, string.Empty));
    }

    /// <summary>
    /// Builds the fact a foreign event stands for in the semantic world.
    /// </summary>
    /// <param name="plan">The plan declaring the event.</param>
    /// <param name="origin">The store the event comes from.</param>
    /// <param name="received">The event delivered from the inbox.</param>
    /// <returns>The fact.</returns>
    /// <exception cref="InvalidOperationException">The event is not one the model declares as foreign from the origin.</exception>
    internal static SemanticFact Fact(SemanticExecutionPlan plan, string origin, ReactorEvent received)
    {
        var contract = plan.Events.Values.SingleOrDefault(candidate => candidate.Origin == origin && SemanticStoredEventNames.Of(candidate) == received.Context.EventType.Id.Value)
            ?? throw new InvalidOperationException($"The inbox of '{origin}' delivered '{received.Context.EventType.Id}', which this model does not declare as a foreign event.");
        var source = SemanticValue.Text(received.Context.EventSourceId.Value);
        using var content = JsonDocument.Parse(received.Content.ToJsonString());

        return new SemanticFact(contract.Id, source, SemanticWorldRebuilder.Values(content.RootElement, contract.Properties, plan.Model.Application))
        {
            Context = plan.Model.SemanticVersion != SemanticVersion.V1 ? new(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), source)) : null,
            Occurred = received.Context.Occurred
        };
    }
}
