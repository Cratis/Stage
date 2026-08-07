// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Stage.Rendering.Cratis.Expressions;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Types;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Represents one read-model property inferred from a projection mapping.
/// </summary>
/// <param name="Name">The PascalCase read-model property name.</param>
/// <param name="Type">The <see cref="ResolvedType"/> of the property.</param>
/// <param name="Attribute">The model-bound projection attribute expressing the mapping, when there is one.</param>
/// <param name="SourcePath">The event property path the value comes from, when the mapping reads one.</param>
public sealed record MappedProperty(string Name, ResolvedType Type, string? Attribute, string? SourcePath);

/// <summary>
/// Resolves a projection <see cref="MappingSyntax"/> to the model-bound attribute that expresses it and the C#
/// type of the read-model property it produces. Keeping the two together matters: the attribute decides which
/// event member is read, and only that member's declared type can type the property.
/// </summary>
public static class ProjectionMapping
{
    static readonly ResolvedType _counter = new("int", false, false, ResolvedTypeKind.Primitive);
    static readonly ResolvedType _unresolved = new("object", false, false, ResolvedTypeKind.Unresolved);

    static readonly Dictionary<string, ResolvedType> _contextPropertyTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Occurred"] = new("DateTimeOffset", false, false, ResolvedTypeKind.Primitive),
        ["EventSourceId"] = new("EventSourceId", false, false, ResolvedTypeKind.Concept),
        ["SequenceNumber"] = new("EventSequenceNumber", false, false, ResolvedTypeKind.Concept),
        ["CorrelationId"] = new("CorrelationId", false, false, ResolvedTypeKind.Concept),
    };

    /// <summary>
    /// Resolves a mapping to the read-model property it produces.
    /// </summary>
    /// <param name="mapping">The <see cref="MappingSyntax"/> to resolve.</param>
    /// <param name="eventName">The Screenplay name of the event the mapping is triggered by, when there is one.</param>
    /// <param name="events">The <see cref="EventPropertyIndex"/> to type event properties against.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> to resolve concept types against.</param>
    /// <param name="diagnostics">Collects anything that could not be rendered faithfully.</param>
    /// <returns>The <see cref="MappedProperty"/>.</returns>
    public static MappedProperty Resolve(
        MappingSyntax mapping,
        string? eventName,
        EventPropertyIndex events,
        ApplicationSet applicationSet,
        ICollection<string> diagnostics)
    {
        var name = Identifiers.ToPascalCase(mapping.Property);

        if (eventName is null)
        {
            diagnostics.Add($"Mapping for '{mapping.Property}' is not bound to an event — rendered without a projection attribute.");
            return new MappedProperty(name, _unresolved, null, null);
        }

        var eventTypeName = Identifiers.ToPascalCase(eventName);

        return mapping switch
        {
            IncrementMappingSyntax => new MappedProperty(name, _counter, $"[Increment<{eventTypeName}>]", null),
            DecrementMappingSyntax => new MappedProperty(name, _counter, $"[Decrement<{eventTypeName}>]", null),
            CountMappingSyntax => new MappedProperty(name, _counter, $"[Count<{eventTypeName}>]", null),
            AddMappingSyntax add => FromSource(name, "AddFrom", eventTypeName, eventName, add.Value, events, applicationSet, diagnostics),
            SubtractMappingSyntax subtract => FromSource(name, "SubtractFrom", eventTypeName, eventName, subtract.Value, events, applicationSet, diagnostics),
            SetMappingSyntax set => FromSource(name, "SetFrom", eventTypeName, eventName, set.Source, events, applicationSet, diagnostics),
            _ => Unsupported(name, mapping, diagnostics),
        };
    }

    static MappedProperty FromSource(
        string name,
        string attributeName,
        string eventTypeName,
        string eventName,
        ExpressionSyntax source,
        EventPropertyIndex events,
        ApplicationSet applicationSet,
        ICollection<string> diagnostics) => source switch
        {
            PathExpressionSyntax path => FromEventProperty(name, attributeName, eventTypeName, eventName, path.Path, events, applicationSet, diagnostics),
            LiteralExpressionSyntax literal => FromLiteral(name, eventTypeName, literal),
            EventSourceIdExpressionSyntax => FromContext(name, eventTypeName, "EventSourceId"),
            EventContextExpressionSyntax context => FromContext(name, eventTypeName, Identifiers.ToPascalCase(context.Path)),
            ContextExpressionSyntax context => FromContext(name, eventTypeName, Identifiers.ToPascalCase(context.Path)),
            _ => UnsupportedSource(name, source, diagnostics),
        };

    static MappedProperty FromEventProperty(
        string name,
        string attributeName,
        string eventTypeName,
        string eventName,
        string sourcePath,
        EventPropertyIndex events,
        ApplicationSet applicationSet,
        ICollection<string> diagnostics)
    {
        var propertyName = Identifiers.ToPascalCase(sourcePath);
        var typeRef = events.TypeOf(eventName, sourcePath);
        var type = typeRef is null ? _unresolved with { SourceName = $"{eventName}.{sourcePath}" } : TypeResolver.Resolve(typeRef, applicationSet);

        if (typeRef is null)
        {
            diagnostics.Add(
                events.Declares(eventName)
                    ? $"Event '{eventName}' declares no property '{sourcePath}' — '{name}' rendered as 'object'."
                    : $"Event '{eventName}' is not declared in this application — '{name}' rendered as 'object'.");
        }

        var diagnostic = TypeResolver.DescribeIfUnresolved(type, $"read model property '{name}'");
        if (diagnostic is not null && typeRef is not null)
        {
            diagnostics.Add(diagnostic);
        }

        // A nameof() reference is a compile-time check that the mapping still matches the event. It can only be
        // emitted when the event is declared here and really carries the property; otherwise the name goes through
        // as a string so the generated file still compiles and the diagnostic above says why.
        var reference = events.Declares(eventName, sourcePath) ? $"nameof({eventTypeName}.{propertyName})" : $"\"{propertyName}\"";
        return new MappedProperty(name, type, $"[{attributeName}<{eventTypeName}>({reference})]", sourcePath);
    }

    static MappedProperty FromLiteral(string name, string eventTypeName, LiteralExpressionSyntax literal)
    {
        var type = literal.Value switch
        {
            bool => new ResolvedType("bool", false, false, ResolvedTypeKind.Primitive),
            string => new ResolvedType("string", false, false, ResolvedTypeKind.Primitive),
            int or long => new ResolvedType("int", false, false, ResolvedTypeKind.Primitive),
            double or decimal => new ResolvedType("decimal", false, false, ResolvedTypeKind.Primitive),
            _ => _unresolved with { SourceName = literal.Value?.ToString() },
        };

        return new MappedProperty(name, type, $"[SetValue<{eventTypeName}>({ExpressionRenderer.Render(literal)})]", null);
    }

    static MappedProperty FromContext(string name, string eventTypeName, string contextProperty)
    {
        var type = _contextPropertyTypes.GetValueOrDefault(contextProperty, _unresolved with { SourceName = $"$eventContext.{contextProperty}" });
        return new MappedProperty(name, type, $"[SetFromContext<{eventTypeName}>(\"{contextProperty}\")]", null);
    }

    static MappedProperty Unsupported(string name, MappingSyntax mapping, ICollection<string> diagnostics)
    {
        diagnostics.Add($"Mapping of type '{mapping.GetType().Name}' for '{mapping.Property}' has no model-bound equivalent — rendered without a projection attribute.");
        return new MappedProperty(name, _unresolved, null, null);
    }

    static MappedProperty UnsupportedSource(string name, ExpressionSyntax source, ICollection<string> diagnostics)
    {
        diagnostics.Add($"Mapping source of type '{source.GetType().Name}' for '{name}' has no model-bound equivalent — rendered without a projection attribute.");
        return new MappedProperty(name, _unresolved, null, null);
    }
}
