// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Projections;

/// <summary>
/// Resolves attribute-safe values and paths without changing the shared semantic types.
/// </summary>
internal static class SemanticFlatModelBoundProjectionValues
{
    internal static bool PathKey(SemanticProjectionKey? key) => key is null or
        SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity or SemanticProjectionEventProperty };

    internal static string KeyPath(SemanticProjectionKey? key, SemanticEventContract @event, SemanticApplicationContext context) => key switch
    {
        SemanticProjectionValueKey { Value: SemanticProjectionEventProperty property } => Path(property.Path, @event, context),
        _ => string.Empty
    };

    internal static string FromArguments(SemanticProjectionFrom from, SemanticEventContract @event, SemanticApplicationContext context)
    {
        var arguments = KeyPath(from.Key, @event, context);
        arguments = AppendParent(arguments, from.ParentKey, @event, context);
        return from.Key is SemanticProjectionValueKey { Value: SemanticProjectionLiteral { Value: SemanticTextValue text } }
            ? Append(arguments, $"ConstantKey = {CSharpCodeBuilder.StringLiteral(text.Value)}")
            : arguments;
    }

    internal static string AppendParent(string arguments, SemanticProjectionKey? parent, SemanticEventContract @event, SemanticApplicationContext context) =>
        parent is null ? arguments : Append(arguments, $"parentKey: {KeyPath(parent, @event, context)}");

    internal static string Append(string arguments, string extra) => arguments.Length == 0 ? extra : $"{arguments}, {extra}";

    internal static string Path(ImmutableArray<SemanticId> path, SemanticEventContract @event, SemanticApplicationContext context)
    {
        if (path.Length == 1)
        {
            return $"nameof({new SemanticTypeSystem(context).EventType(@event)}.{Identifiers.ToPascalCase(@event.Properties.Single(property => property.Id == path[0]).Name)})";
        }

        IReadOnlyList<SemanticProperty> properties = @event.Properties;
        var segments = new List<string>();
        foreach (var id in path)
        {
            var property = properties.Single(candidate => candidate.Id == id);
            segments.Add(Identifiers.ToPascalCase(property.Name));
            properties = context.Types.TryGetValue(property.Type.Target, out var composite) ? composite.Properties : [];
        }
        return CSharpCodeBuilder.StringLiteral(string.Join('.', segments));
    }

    internal static bool LiteralSupported(SemanticProjectionLiteral literal, SemanticProperty property, SemanticApplicationContext context)
    {
        var type = property.Type;
        if (!ConstantTarget(type, context)) return false;
        return (literal.Value, Primitive(type, context)) switch
        {
            (SemanticNullValue, _) => type.IsOptional,
            (SemanticTextValue, SemanticPrimitiveType.Text) => true,
            (SemanticNumberValue, SemanticPrimitiveType.WholeNumber) => true,
            (SemanticBooleanValue, SemanticPrimitiveType.Boolean) => true,
            _ => false
        };
    }

    internal static string Literal(SemanticProjectionLiteral literal, SemanticProperty property, SemanticApplicationContext context) =>
        new SemanticTypeSystem(context).Value(literal.Value, new(SemanticTypeReferenceKind.Primitive, Primitive(property.Type, context), default, property.Type.IsOptional, false));

    static bool ConstantTarget(SemanticTypeReference type, SemanticApplicationContext context) =>
        !type.IsCollection && type.Kind is SemanticTypeReferenceKind.Primitive or SemanticTypeReferenceKind.Concept &&
        (type.Kind != SemanticTypeReferenceKind.Concept || context.Concepts[type.Target].Values.Length == 0);

    static SemanticPrimitiveType Primitive(SemanticTypeReference type, SemanticApplicationContext context) =>
        type.Kind == SemanticTypeReferenceKind.Concept ? context.Concepts[type.Target].Primitive : type.Primitive;
}
