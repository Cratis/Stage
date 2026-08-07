// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Types;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Renders a Screenplay <see cref="EventSyntax"/> as an <c>[EventType]</c> record. Every slice type may declare
/// events — a State View declaring the events it projects, a reactor slice declaring the ones it reacts to — so
/// this is shared rather than owned by the State Change renderer.
/// </summary>
public static class EventRenderer
{
    /// <summary>
    /// Renders an event into a builder.
    /// </summary>
    /// <param name="builder">The <see cref="CSharpCodeBuilder"/> to render into.</param>
    /// <param name="event">The <see cref="EventSyntax"/> to render.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> to resolve property types against.</param>
    /// <param name="diagnostics">Collects anything that could not be rendered faithfully.</param>
    public static void Render(CSharpCodeBuilder builder, EventSyntax @event, ApplicationSet applicationSet, ICollection<string> diagnostics)
    {
        var typeName = Identifiers.ToPascalCase(@event.Name);
        var parameters = string.Join(", ", @event.Properties.Select(property => RenderParameter(property, @event.Name, applicationSet, diagnostics)));

        builder.BlankLine().Using("Cratis.Chronicle.Events").Summary($"Emitted when {Identifiers.ToWords(@event.Name)}.");
        foreach (var property in @event.Properties)
        {
            builder.Line($"/// <param name=\"{Identifiers.ToPascalCase(property.Name)}\">The {Identifiers.ToWords(property.Name)}.</param>");
        }

        builder.Attribute("EventType").Line($"public record {typeName}({parameters});");
    }

    /// <summary>
    /// Gets the Screenplay names an event's properties reference, for import resolution.
    /// </summary>
    /// <param name="events">The events to walk.</param>
    /// <returns>The referenced names.</returns>
    public static IEnumerable<string> ReferencedNames(IEnumerable<EventSyntax> events) =>
        events.SelectMany(@event => @event.Properties).Select(property => property.Type.Name);

    static string RenderParameter(PropertySyntax property, string eventName, ApplicationSet applicationSet, ICollection<string> diagnostics)
    {
        var resolved = TypeResolver.Resolve(property.Type, applicationSet);
        var diagnostic = TypeResolver.DescribeIfUnresolved(resolved, $"property '{property.Name}' of event '{eventName}'");
        if (diagnostic is not null)
        {
            diagnostics.Add(diagnostic);
        }

        return $"{resolved.ToTypeSyntax()} {Identifiers.ToPascalCase(property.Name)}";
    }
}
