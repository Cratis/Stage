// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Resolves ESM type and value contracts into deterministic C# syntax.
/// </summary>
/// <param name="context">The indexed semantic application.</param>
internal sealed class SemanticTypeSystem(SemanticApplicationContext context)
{
    /// <summary>
    /// Gets the C# primitive represented by a semantic primitive.
    /// </summary>
    /// <param name="primitive">The semantic primitive.</param>
    /// <returns>The C# primitive type syntax.</returns>
    public static string Primitive(SemanticPrimitiveType primitive) => primitive switch
    {
        SemanticPrimitiveType.Uuid => "Guid",
        SemanticPrimitiveType.Text => "string",
        SemanticPrimitiveType.WholeNumber => "int",
        SemanticPrimitiveType.DecimalNumber => "decimal",
        SemanticPrimitiveType.Boolean => "bool",
        SemanticPrimitiveType.Date => "DateOnly",
        SemanticPrimitiveType.DateTime => "DateTimeOffset",
        _ => "object"
    };

    /// <summary>
    /// Gets the sentinel expression for a primitive.
    /// </summary>
    /// <param name="primitive">The primitive.</param>
    /// <returns>The sentinel expression.</returns>
    public static string NotSet(SemanticPrimitiveType primitive) => primitive switch
    {
        SemanticPrimitiveType.Uuid => "Guid.Empty",
        SemanticPrimitiveType.Text => "string.Empty",
        SemanticPrimitiveType.WholeNumber => "0",
        SemanticPrimitiveType.DecimalNumber => "0m",
        SemanticPrimitiveType.Boolean => "false",
        SemanticPrimitiveType.Date => "DateOnly.MinValue",
        SemanticPrimitiveType.DateTime => "DateTimeOffset.MinValue",
        _ => "default!"
    };

    /// <summary>
    /// Gets the C# type syntax for a semantic type reference.
    /// </summary>
    /// <param name="reference">The semantic type reference.</param>
    /// <returns>The C# type syntax.</returns>
    public string Type(SemanticTypeReference reference)
    {
        var scalar = reference.Kind switch
        {
            SemanticTypeReferenceKind.Primitive => Primitive(reference.Primitive),
            SemanticTypeReferenceKind.Concept => Identifiers.ToPascalCase(context.Concepts[reference.Target].Name),
            SemanticTypeReferenceKind.CompositeType => Identifiers.ToPascalCase(context.Types[reference.Target].Name),
            _ => "object"
        };

        var type = reference.IsCollection ? $"IReadOnlyList<{scalar}>" : scalar;
        return reference.IsOptional ? $"{type}?" : type;
    }

    /// <summary>
    /// Renders a concrete semantic value according to its declared type.
    /// </summary>
    /// <param name="value">The semantic value.</param>
    /// <param name="type">The declared type.</param>
    /// <returns>The C# value expression.</returns>
    public string Value(SemanticValue value, SemanticTypeReference type)
    {
        if (value is SemanticNullValue)
        {
            return "null";
        }

        if (type.IsCollection && value is SemanticArrayValue array)
        {
            var elementType = type with { IsCollection = false };
            return $"[{string.Join(", ", array.Values.Select(_ => Value(_, elementType)))}]";
        }

        if (type.Kind == SemanticTypeReferenceKind.Concept)
        {
            var concept = context.Concepts[type.Target];
            return $"new {Identifiers.ToPascalCase(concept.Name)}({PrimitiveValue(value, concept.Primitive)})";
        }

        if (type.Kind == SemanticTypeReferenceKind.CompositeType && value is SemanticCompositeValue composite)
        {
            var semanticType = context.Types[type.Target];
            var values = semanticType.Properties.Select(property =>
                Value(composite.Properties.Single(_ => _.TargetProperty == property.Id).Value, property.Type));
            return $"new {Identifiers.ToPascalCase(semanticType.Name)}({string.Join(", ", values)})";
        }

        return PrimitiveValue(value, type.Primitive);
    }

    static string PrimitiveValue(SemanticValue value, SemanticPrimitiveType primitive) => (value, primitive) switch
    {
        (SemanticTextValue text, SemanticPrimitiveType.Uuid) => $"Guid.Parse({Literal(text.Value)})",
        (SemanticTextValue text, SemanticPrimitiveType.Date) => $"DateOnly.Parse({Literal(text.Value)}, CultureInfo.InvariantCulture)",
        (SemanticTextValue text, SemanticPrimitiveType.DateTime) => $"DateTimeOffset.Parse({Literal(text.Value)}, CultureInfo.InvariantCulture)",
        (SemanticTextValue text, _) => Literal(text.Value),
        (SemanticNumberValue number, SemanticPrimitiveType.DecimalNumber) => $"{number.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}m",
        (SemanticNumberValue number, _) => number.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
        (SemanticBooleanValue boolean, _) => boolean.Value ? "true" : "false",
        _ => "default!"
    };

    static string Literal(string value) =>
        $"\"{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
}
