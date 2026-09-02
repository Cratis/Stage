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

    /// <summary>
    /// Resolves a mapping inside a <c>join</c> block's <c>with</c> to the read-model property it produces, carrying
    /// the <c>[Join]</c> attribute that pulls the value in from the joined event instead of the local one.
    /// </summary>
    /// <param name="mapping">The <see cref="MappingSyntax"/> to resolve.</param>
    /// <param name="eventName">The Screenplay name of the joined event.</param>
    /// <param name="on">The Screenplay name of the read-model property the join matches on.</param>
    /// <param name="modelProperties">
    /// The read-model property names inferred so far, deciding whether the join key can be referenced with
    /// <c>nameof</c> — a name that is not a property of the record would not bind.
    /// </param>
    /// <param name="events">The <see cref="EventPropertyIndex"/> to type event properties against.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> to resolve concept types against.</param>
    /// <param name="diagnostics">Collects anything that could not be rendered faithfully.</param>
    /// <returns>The <see cref="MappedProperty"/>.</returns>
    public static MappedProperty ResolveJoined(
        MappingSyntax mapping,
        string eventName,
        string on,
        IReadOnlySet<string> modelProperties,
        EventPropertyIndex events,
        ApplicationSet applicationSet,
        ICollection<string> diagnostics)
    {
        var property = Resolve(mapping, eventName, events, applicationSet, diagnostics);

        // A join reads a property off the joined event and nothing else — a counter, a literal or a context
        // value has no join to express it, so the property is kept and the mapping is reported rather than
        // rendered as a join it does not mean.
        if (mapping is not SetMappingSyntax || property.SourcePath is null)
        {
            diagnostics.Add($"Mapping for '{mapping.Property}' in the join on '{on}' does not read a property of '{eventName}' — a join maps only from an event property, so it is rendered without a projection attribute.");
            return property with { Attribute = null };
        }

        var eventTypeName = Identifiers.ToPascalCase(eventName);
        var onName = Identifiers.ToPascalCase(on);
        string onReference;

        if (modelProperties.Contains(onName))
        {
            onReference = $"nameof({onName})";
        }
        else
        {
            // A join matches on a read-model property. When no earlier block puts that property on the record the
            // name can only go through as a literal, which compiles but is not checked until the projection is
            // built — so the reason is recorded here instead of surfacing later as a projection that never matches.
            diagnostics.Add(
                $"The join on '{on}' matches '{onName}', which no block maps onto the read model — it is referenced by name and only checked when the projection is built.");
            onReference = $"\"{onName}\"";
        }

        var eventProperty = EventPropertyReference(eventName, property.SourcePath, events);

        return property with { Attribute = $"[Join<{eventTypeName}>(on: {onReference}, eventPropertyName: {eventProperty})]" };
    }

    /// <summary>
    /// Resolves a mapping inside an <c>all</c> or <c>every</c> block to the read-model property it produces.
    /// </summary>
    /// <param name="mapping">The <see cref="MappingSyntax"/> to resolve.</param>
    /// <param name="scope">The <see cref="GlobalMappingScope"/> the mapping was declared in.</param>
    /// <param name="diagnostics">Collects anything that could not be rendered faithfully.</param>
    /// <returns>The <see cref="MappedProperty"/>.</returns>
    public static MappedProperty ResolveGlobal(MappingSyntax mapping, GlobalMappingScope scope, ICollection<string> diagnostics)
    {
        var name = Identifiers.ToPascalCase(mapping.Property);
        var keyword = Keyword(scope);

        if (mapping is not SetMappingSyntax set)
        {
            // A counter still has an unambiguous type even though [FromAll]/[FromEvery] carry no counting, so
            // the property it names is declared as the counter it is rather than as an untyped placeholder.
            diagnostics.Add($"Mapping of type '{mapping.GetType().Name}' for '{mapping.Property}' in an '{keyword}' block has no model-bound equivalent — rendered without a projection attribute.");
            var counted = mapping is IncrementMappingSyntax or DecrementMappingSyntax or CountMappingSyntax;
            return new MappedProperty(name, counted ? _counter : _unresolved, null, null);
        }

        return set.Source switch
        {
            EventSourceIdExpressionSyntax => GlobalContext(name, scope, "EventSourceId"),
            EventContextExpressionSyntax context => GlobalContext(name, scope, Identifiers.ToPascalCase(context.Path)),
            ContextExpressionSyntax context => GlobalContext(name, scope, Identifiers.ToPascalCase(context.Path)),
            PathExpressionSyntax path => GlobalEventProperty(name, scope, keyword, path.Path, diagnostics),
            _ => UnsupportedSource(name, set.Source, diagnostics),
        };
    }

    /// <summary>
    /// Renders the reference an attribute uses to name a property on an event.
    /// </summary>
    /// <param name="eventName">The Screenplay name of the event.</param>
    /// <param name="sourcePath">The Screenplay name of the property on that event.</param>
    /// <param name="events">The <see cref="EventPropertyIndex"/> deciding whether the reference would bind.</param>
    /// <returns>A <c>nameof</c> reference when the event really declares the property, a string literal otherwise.</returns>
    /// <remarks>
    /// A <c>nameof()</c> reference is a compile-time check that the mapping still matches the event. It can only be
    /// emitted when the event is declared here and really carries the property; otherwise the name goes through as
    /// a string so the generated file still compiles and the diagnostic raised alongside it says why.
    /// </remarks>
    public static string EventPropertyReference(string eventName, string sourcePath, EventPropertyIndex events)
    {
        var propertyName = Identifiers.ToPascalCase(sourcePath);
        return events.Declares(eventName, sourcePath)
            ? $"nameof({Identifiers.ToPascalCase(eventName)}.{propertyName})"
            : $"\"{propertyName}\"";
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

    static string Keyword(GlobalMappingScope scope) => scope == GlobalMappingScope.All ? "all" : "every";

    // [FromAll] targets a property and nothing else, so on a positional record it has to be written with an
    // explicit 'property:' target; [FromEvery] targets a parameter as well and is written like every other
    // mapping attribute. Both are emitted with named arguments — their first parameter is not the same one.
    static string GlobalAttribute(GlobalMappingScope scope, string argument) =>
        scope == GlobalMappingScope.All ? $"[property: FromAll({argument})]" : $"[FromEvery({argument})]";

    static MappedProperty GlobalContext(string name, GlobalMappingScope scope, string contextProperty)
    {
        var type = _contextPropertyTypes.GetValueOrDefault(contextProperty, _unresolved with { SourceName = $"$eventContext.{contextProperty}" });
        return new MappedProperty(name, type, GlobalAttribute(scope, $"contextProperty: \"{contextProperty}\""), null);
    }

    static MappedProperty GlobalEventProperty(string name, GlobalMappingScope scope, string keyword, string sourcePath, ICollection<string> diagnostics)
    {
        // An 'all' or 'every' block is bound to no single event, so there is no declaration to type the property
        // from. The attribute still names the event property it reads; the property itself becomes 'object'.
        diagnostics.Add($"Mapping '{name}' in an '{keyword}' block reads event property '{sourcePath}' from no single event — '{name}' rendered as 'object'.");
        return new MappedProperty(
            name,
            _unresolved with { SourceName = sourcePath },
            GlobalAttribute(scope, $"property: \"{Identifiers.ToPascalCase(sourcePath)}\""),
            null);
    }

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

        var reference = EventPropertyReference(eventName, sourcePath, events);
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
