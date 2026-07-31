// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Runtime;

/// <summary>
/// Defines the system that appends the events a modeled command produces to Chronicle.
/// </summary>
public interface IAppendProducedEvents
{
    /// <summary>
    /// Appends events to the event log of the running event store.
    /// </summary>
    /// <param name="eventSourceId">The event source to append to.</param>
    /// <param name="events">The events to append, in order.</param>
    /// <param name="identity">The identity that caused the command, recorded as the causing identity of the events.</param>
    /// <returns>Awaitable task.</returns>
    Task Append(string eventSourceId, IReadOnlyList<ProducedEventPayload> events, IReadOnlyDictionary<string, string> identity);
}
