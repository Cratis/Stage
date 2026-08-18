// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Renders the uniqueness a slice declares onto the events it is declared against, as Chronicle
/// <c>[Unique]</c> attributes.
/// </summary>
/// <remarks>
/// Uniqueness is the one invariant a rendered application cannot enforce for itself. A read-then-write check in a
/// handler loses the race the constraint exists to win, so Chronicle enforces it at append time and the document
/// has to reach that mechanism or state nothing. Until this rendered, a document that carefully declared which
/// values must stay unique produced an application where none of them did.
/// <para>
/// A constraint backed by a file is not rendered — it points at code outside the document, and there is nothing
/// to read to know what it asserts. <see cref="UnrenderedConstructs"/> reports those, and only those.
/// </para>
/// </remarks>
public static class ConstraintRenderer
{
    /// <summary>
    /// Gets the attribute an event type carries, when the slice declares the whole event unique.
    /// </summary>
    /// <param name="constraints">The constraints the slice declares.</param>
    /// <param name="event">The name of the event being rendered.</param>
    /// <returns>The attribute text without brackets, or <see langword="null"/> when the event carries none.</returns>
    public static string? ForEvent(IEnumerable<ConstraintSyntax> constraints, string @event) =>
        constraints.OfType<UniqueEventConstraintSyntax>()
            .Where(constraint => Names(constraint.Event, @event))
            .Select(constraint => Attribute(constraint.Name))
            .FirstOrDefault();

    /// <summary>
    /// Gets the attribute a property carries, when the slice declares that property unique.
    /// </summary>
    /// <param name="constraints">The constraints the slice declares.</param>
    /// <param name="event">The name of the event being rendered.</param>
    /// <param name="property">The name of the property being rendered.</param>
    /// <returns>The attribute text, targeted at the property, or <see langword="null"/> when it carries none.</returns>
    /// <remarks>
    /// The <c>property:</c> target is load-bearing. An event renders as a positional record, and an attribute on a
    /// positional parameter lands on the parameter, which Chronicle never reads — it looks at the property the
    /// record generates from it.
    /// </remarks>
    public static string? ForProperty(IEnumerable<ConstraintSyntax> constraints, string @event, string property) =>
        constraints.OfType<UniquePropertyConstraintSyntax>()
            .Where(constraint => Names(constraint.Event, @event) && Names(constraint.Property, property))
            .Select(constraint => $"[property: {Attribute(constraint.Name)}]")
            .FirstOrDefault();

    /// <summary>
    /// Determines whether a constraint has a rendered equivalent, so what does not can be reported.
    /// </summary>
    /// <param name="constraint">The constraint to consider.</param>
    /// <param name="events">The events the slice declares, which are the only ones an attribute can be placed on.</param>
    /// <returns>True when rendering places it on an event.</returns>
    public static bool IsRendered(ConstraintSyntax constraint, IEnumerable<EventSyntax> events) => constraint switch
    {
        UniqueEventConstraintSyntax unique => events.Any(@event => Names(@event.Name, unique.Event)),
        UniquePropertyConstraintSyntax unique => events.Any(@event =>
            Names(@event.Name, unique.Event) && @event.Properties.Any(property => Names(property.Name, unique.Property))),
        _ => false
    };

    // The constraint's own name is carried through so a violation reports the name the document gave it rather
    // than one Chronicle derives, which is what a caller matches on to tell two violations apart.
    static string Attribute(string name) => $"Unique(\"{name}\")";

    static bool Names(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
