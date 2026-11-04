// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// The application's own public events and the concepts and composite types they need, which render into the contracts project
/// so that other applications can reference the contract without the application.
/// </summary>
internal sealed class SemanticEventContracts
{
    readonly HashSet<SemanticId> _events;
    readonly HashSet<SemanticId> _concepts;
    readonly HashSet<SemanticId> _types;

    SemanticEventContracts(IReadOnlyList<SemanticEventContract> events, HashSet<SemanticId> concepts, HashSet<SemanticId> types)
    {
        Events = events;
        _events = [.. events.Select(@event => @event.Id)];
        _concepts = concepts;
        _types = types;
    }

    /// <summary>
    /// Gets a value indicating whether the application declares any own public event.
    /// </summary>
    public bool Any => Events.Count > 0;

    /// <summary>
    /// Gets the own public events in declaration order.
    /// </summary>
    public IReadOnlyList<SemanticEventContract> Events { get; }

    /// <summary>
    /// Gets whether an event renders into the contracts project.
    /// </summary>
    /// <param name="event">The event identity.</param>
    /// <returns>True when the event is an own public event.</returns>
    public bool ContainsEvent(SemanticId @event) => _events.Contains(@event);

    /// <summary>
    /// Gets whether a concept renders into the contracts project.
    /// </summary>
    /// <param name="concept">The concept identity.</param>
    /// <returns>True when an own public event needs the concept.</returns>
    public bool ContainsConcept(SemanticId concept) => _concepts.Contains(concept);

    /// <summary>
    /// Gets whether a composite type renders into the contracts project.
    /// </summary>
    /// <param name="type">The composite type identity.</param>
    /// <returns>True when an own public event needs the type.</returns>
    public bool ContainsType(SemanticId type) => _types.Contains(type);

    /// <summary>
    /// Gets the events that need a concept carrying metadata the contracts project cannot reference.
    /// </summary>
    /// <param name="context">The indexed application.</param>
    /// <returns>The event and the concept that block the contracts project.</returns>
    public IEnumerable<(SemanticEventContract Event, SemanticConcept Concept)> Blockers(SemanticApplicationContext context)
    {
        foreach (var @event in Events)
        {
            var closure = Close(context, [@event]);
            foreach (var concept in closure.Concepts.Select(id => context.Concepts[id]).Where(NeedsApplicationMetadata))
            {
                yield return (@event, concept);
            }
        }
    }

    internal static SemanticEventContracts From(SemanticApplicationContext context)
    {
        var events = context.Events.Values.Where(@event => @event.Visibility == SemanticEventVisibility.Public && @event.Origin is null).ToArray();
        if (events.Length == 0) return new([], [], []);
        var closure = Close(context, events);
        return new(events, closure.Concepts, closure.Types);
    }

    // A secret-only value is also kept out of the causation chain, which takes an Arc attribute the contracts project does not reference.
    // A personal classification takes precedence and renders only the PII attribute.
    static bool NeedsApplicationMetadata(SemanticConcept concept) => concept.Compliance is { Personal: null, Secret: not null };

    static SemanticTypeClosure Close(SemanticApplicationContext context, IEnumerable<SemanticEventContract> events)
    {
        var closure = new SemanticTypeClosure(context);
        foreach (var @event in events) closure.Properties(@event.Properties);

        // A concept declares an implicit conversion from each concept a projection fills it from, so those travel with it.
        var pending = new Stack<SemanticId>(closure.Concepts);
        while (pending.Count > 0)
        {
            foreach (var source in SemanticProjectionConversions.Into(context.Concepts[pending.Pop()], context))
            {
                if (closure.Concepts.Add(source.Id)) pending.Push(source.Id);
            }
        }

        return closure;
    }
}
