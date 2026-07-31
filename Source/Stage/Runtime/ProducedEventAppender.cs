// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Identities;
using Microsoft.Extensions.Logging;
using ChronicleEvents = Cratis.Chronicle.Contracts.Events;
using ChronicleEventSequences = Cratis.Chronicle.Contracts.EventSequences;
using ChronicleIdentities = Cratis.Chronicle.Contracts.Identities;

namespace Cratis.Stage.Runtime;

/// <summary>
/// Appends the events a modeled command produces straight through Chronicle's event sequence contract. The engine has
/// no compiled event types to hand the client SDK — the model's event names are the Chronicle event type identifiers
/// and the payloads are JSON — so the contract is the right level to append at.
/// </summary>
/// <param name="client">The <see cref="IChronicleClient"/> for the running event store.</param>
/// <param name="eventStore">The name of the event store the engine runs against.</param>
/// <param name="logger">The logger used to report a failed append.</param>
public sealed class ProducedEventAppender(IChronicleClient client, StageEventStoreName eventStore, ILogger<ProducedEventAppender> logger) : IAppendProducedEvents
{
    const uint FirstGeneration = 1;

    /// <inheritdoc/>
    public async Task Append(string eventSourceId, IReadOnlyList<ProducedEventPayload> events, IReadOnlyDictionary<string, string> identity)
    {
        if (events.Count == 0)
        {
            return;
        }

        var store = await client.GetEventStore(eventStore.Value);
        await store.Connection.Connect();
        var accessor = (IChronicleServicesAccessor)store.Connection;

        var response = await accessor.Services.EventSequences.AppendMany(new ChronicleEventSequences.AppendManyRequest
        {
            EventStore = store.Name,
            Namespace = EventStoreNamespaceName.Default,
            EventSequenceId = EventSequenceId.Log,
            CausedBy = CausedBy(identity),
            Events =
            [
                .. events.Select(@event => new ChronicleEvents.EventToAppend
                {
                    EventSourceId = eventSourceId,
                    EventType = new ChronicleEvents.EventType { Id = @event.EventType, Generation = FirstGeneration },
                    Content = @event.Content.ToJsonString(),
                    Tags = @event.Tags,
                })
            ],
        });

        if (response.ConstraintViolations.Count > 0)
        {
            ProducedEventAppenderLogging.ConstraintViolations(logger, eventSourceId, response.ConstraintViolations.Count);
        }
    }

    // The kernel requires a causing identity on every append. An anonymous caller is genuinely unknown rather than
    // the platform itself acting, so the Unknown sentinel is the honest default.
    static ChronicleIdentities.Identity CausedBy(IReadOnlyDictionary<string, string> identity) =>
        new()
        {
            Subject = Value(identity, "subject", Identity.Unknown.Subject),
            Name = Value(identity, "name", Identity.Unknown.Name),
            UserName = Value(identity, "userName", Identity.Unknown.UserName),
        };

    static string Value(IReadOnlyDictionary<string, string> identity, string key, string fallback) =>
        identity.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value) ? value : fallback;
}
