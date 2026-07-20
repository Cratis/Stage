// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Stage.Contracts.Constraints;
using Cratis.Stage.Contracts.Events;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Converts the Screenplay events of a slice into Stage <see cref="EventDefinition"/> records, attaching the slice's
/// uniqueness constraints to the events they reference. File-backed constraints have no Stage equivalent and are skipped.
/// </summary>
public static class EventConverter
{
    /// <summary>
    /// Converts a slice's events and constraints into Stage event definitions.
    /// </summary>
    /// <param name="events">The event declarations.</param>
    /// <param name="constraints">The constraint declarations on the slice.</param>
    /// <param name="schema">The schema synthesizer.</param>
    /// <param name="slicePath">The fully-qualified slice path, used to derive stable identifiers.</param>
    /// <returns>The Stage event definitions.</returns>
    public static IReadOnlyList<EventDefinition> Convert(
        IEnumerable<EventSyntax> events,
        IEnumerable<ConstraintSyntax> constraints,
        SchemaSynthesizer schema,
        string slicePath)
    {
        var definitions = events.Select(@event => new EventDefinition(
            DeterministicId.From($"{slicePath}.event.{@event.Name}"),
            @event.Name,
            string.Empty,
            schema.ForProperties(@event.Properties),
            UniqueEventTypeConstraint: null,
            UniqueConstraint: null)).ToList();

        foreach (var constraint in constraints)
        {
            ApplyConstraint(definitions, constraint);
        }

        return definitions;
    }

    static void ApplyConstraint(List<EventDefinition> definitions, ConstraintSyntax constraint)
    {
        switch (constraint)
        {
            case UniquePropertyConstraintSyntax unique:
                Replace(definitions, unique.Event, definition => definition with
                {
                    UniqueConstraint = new UniqueConstraint(
                        unique.Name,
                        string.Empty,
                        IgnoreCasing: false,
                        [new UniqueConstraintEvent(unique.Event, [unique.Property])],
                        RemovedWith: null)
                });
                break;

            case UniqueEventConstraintSyntax uniqueEvent:
                Replace(definitions, uniqueEvent.Event, definition => definition with
                {
                    UniqueEventTypeConstraint = new UniqueEventTypeConstraint(
                        uniqueEvent.Name,
                        string.Empty,
                        uniqueEvent.Event,
                        RemovedWith: null)
                });
                break;

            default:
                // FileConstraintSyntax (and any other) references external code with no Stage representation — skip.
                break;
        }
    }

    static void Replace(List<EventDefinition> definitions, string eventName, Func<EventDefinition, EventDefinition> update)
    {
        var index = definitions.FindIndex(definition => string.Equals(definition.Name, eventName, StringComparison.Ordinal));
        if (index >= 0)
        {
            definitions[index] = update(definitions[index]);
        }
    }
}
