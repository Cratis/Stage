// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Specifications;

/// <summary>
/// Renders what a specification asserts about the events its command appended.
/// </summary>
public static class SpecificationAssertions
{
    /// <summary>
    /// Renders the event source the appended events are asserted against — the value the specification states
    /// for the command's own identifier.
    /// </summary>
    /// <param name="when">The command the specification exercises.</param>
    /// <param name="command">The declared command.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> to resolve the identifier type against.</param>
    /// <param name="diagnostics">Collects anything that could not be rendered faithfully.</param>
    /// <returns>The rendered event source id.</returns>
    /// <remarks>
    /// A command appends to the event source its identifier names, so the value the specification states for that
    /// property is the one the assertion filters on. A specification that states no value for it has not said
    /// which event source it means; the assertion is rendered against the empty one, which fails rather than
    /// passing on the wrong stream.
    /// </remarks>
    public static string Of(
        SpecificationCommandSyntax when, CommandSyntax command, ApplicationSet applicationSet, ICollection<string> diagnostics)
    {
        var identifier = command.Properties.FirstOrDefault(property => property.IsIdentifier);
        if (identifier is null)
        {
            diagnostics.Add($"Command '{command.Name}' declares no identifier, so the appended events are asserted against no event source.");
            return "EventSourceId.Unspecified";
        }

        var stated = when.Values.FirstOrDefault(value => string.Equals(value.Property, identifier.Name, StringComparison.OrdinalIgnoreCase));
        if (stated is null)
        {
            diagnostics.Add(
                $"The specification states no value for '{identifier.Name}', which is what says which event source " +
                "the appended events belong to.");
            return "EventSourceId.Unspecified";
        }

        var value = SpecificationValues.For(identifier, when.Values, command.Name, applicationSet, diagnostics);
        if (value == "default!")
        {
            diagnostics.Add(
                $"The value stated for '{identifier.Name}' cannot be rendered, so the appended events are asserted " +
                "against no event source.");
            return "EventSourceId.Unspecified";
        }

        // Rendered through the same conversion the command's own argument takes, then to string. Arc resolves the
        // event source id from the constructed identity, and a Guid's canonical form is lowercase - asserting
        // against the document's raw text would never match an id the command appended under, whatever casing the
        // document happened to use.
        return $"new EventSourceId({value}.ToString())";
    }

    /// <summary>
    /// Renders the predicate narrowing an appended-event assertion to the values the specification states, or an
    /// empty string when it states none beyond the event type.
    /// </summary>
    /// <param name="event">The expected event.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> to resolve the event's property types against.</param>
    /// <param name="diagnostics">Collects anything that could not be rendered faithfully.</param>
    /// <returns>The rendered predicate, prefixed with a comma, or an empty string.</returns>
    public static string Predicate(SpecificationEventSyntax @event, ApplicationSet applicationSet, ICollection<string> diagnostics)
    {
        var declared = applicationSet.Events.GetValueOrDefault(@event.EventType);
        if (declared is null)
        {
            diagnostics.Add($"Event '{@event.EventType}' is not declared in this application, so only its type is asserted.");
            return string.Empty;
        }

        var pairs = @event.Values
            .Select(value => (Value: value, Property: declared.Properties.FirstOrDefault(
                property => string.Equals(property.Name, value.Property, StringComparison.OrdinalIgnoreCase))))
            .ToArray();

        foreach (var undeclared in pairs.Where(pair => pair.Property is null))
        {
            diagnostics.Add(
                $"The specification states '{undeclared.Value.Property}' on '{@event.EventType}', which the event does " +
                "not declare — it is not asserted.");
        }

        var comparisons = pairs
            .Where(pair => pair.Property is not null)
            .Select(pair => Comparison(pair.Value, pair.Property!, @event.EventType, applicationSet, diagnostics))
            .Where(comparison => comparison is not null)
            .ToArray();

        return comparisons.Length == 0 ? string.Empty : $", @event => {string.Join(" && ", comparisons)}";
    }

    static string? Comparison(
        PropertyMappingSyntax value,
        PropertySyntax property,
        string eventType,
        ApplicationSet applicationSet,
        ICollection<string> diagnostics)
    {
        if (value.Source is not LiteralExpressionSyntax literal)
        {
            diagnostics.Add(
                $"'{property.Name}' of '{eventType}' is stated as a {value.Source.GetType().Name}, which an assertion " +
                "cannot compare against — it is not asserted.");
            return null;
        }

        var rendered = SpecificationValues.Literal(literal.Value, property.Type, property.Name, eventType, applicationSet, diagnostics);
        return rendered == "default!" ? null : $"@event.{Identifiers.ToPascalCase(property.Name)} == {rendered}";
    }
}
