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
    readonly bool _usesLongWholeNumbers = context.Request.Model.SemanticVersion.IsAtLeast(SemanticVersion.V8);

    /// <summary>
    /// Determines whether a rendered declaration names a shared concept or composite type.
    /// </summary>
    /// <param name="type">The declared type, including optional and collection modifiers.</param>
    /// <returns>Whether the declaration requires the Common namespace.</returns>
    public static bool DeclarationNeedsCommon(SemanticTypeReference type) =>
        type.Kind is SemanticTypeReferenceKind.Concept or SemanticTypeReferenceKind.CompositeType;

    /// <summary>
    /// Determines whether rendering a value actually names a shared constructor.
    /// </summary>
    /// <param name="value">The value to render.</param>
    /// <param name="type">The declared type.</param>
    /// <returns>Whether the value expression requires the Common namespace.</returns>
    public static bool ValueNeedsCommon(SemanticValue value, SemanticTypeReference type)
    {
        if (value is SemanticNullValue)
        {
            return false;
        }

        if (type.IsCollection && value is SemanticArrayValue array)
        {
            return array.Values.Any(element => ValueNeedsCommon(element, type with { IsCollection = false }));
        }

        return type.Kind == SemanticTypeReferenceKind.Concept ||
            (type.Kind == SemanticTypeReferenceKind.CompositeType && value is SemanticCompositeValue);
    }

    /// <summary>
    /// Gets the C# primitive represented by a semantic primitive.
    /// </summary>
    /// <param name="primitive">The semantic primitive.</param>
    /// <returns>The C# primitive type syntax.</returns>
    /// <exception cref="UnsupportedSemanticRendering">The primitive is not handled by this renderer.</exception>
    public string Primitive(SemanticPrimitiveType primitive) => primitive switch
    {
        SemanticPrimitiveType.Uuid => "global::System.Guid",
        SemanticPrimitiveType.Text => "string",
        SemanticPrimitiveType.WholeNumber => _usesLongWholeNumbers ? "long" : "int",
        SemanticPrimitiveType.DecimalNumber => "decimal",
        SemanticPrimitiveType.Boolean => "bool",
        SemanticPrimitiveType.Date => "global::System.DateOnly",
        SemanticPrimitiveType.DateTime => "global::System.DateTimeOffset",
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticPrimitiveType), primitive)
    };

    /// <summary>
    /// Gets the sentinel expression for a primitive.
    /// </summary>
    /// <param name="primitive">The primitive.</param>
    /// <returns>The sentinel expression.</returns>
    /// <exception cref="UnsupportedSemanticRendering">The primitive is not handled by this renderer.</exception>
    public string NotSet(SemanticPrimitiveType primitive) => primitive switch
    {
        SemanticPrimitiveType.Uuid => "global::System.Guid.Empty",
        SemanticPrimitiveType.Text => "string.Empty",
        SemanticPrimitiveType.WholeNumber => _usesLongWholeNumbers ? "0L" : "0",
        SemanticPrimitiveType.DecimalNumber => "0m",
        SemanticPrimitiveType.Boolean => "false",
        SemanticPrimitiveType.Date => "global::System.DateOnly.MinValue",
        SemanticPrimitiveType.DateTime => "global::System.DateTimeOffset.MinValue",
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticPrimitiveType), primitive)
    };

    /// <summary>
    /// Gets the C# type syntax for a semantic type reference.
    /// </summary>
    /// <param name="reference">The semantic type reference.</param>
    /// <param name="reducerInput">Whether this is a reducer-visible event or state property.</param>
    /// <returns>The C# type syntax.</returns>
    /// <exception cref="UnsupportedSemanticRendering">The type reference is not handled by this renderer.</exception>
    public string Type(SemanticTypeReference reference, bool reducerInput = false)
    {
        var scalar = reference.Kind switch
        {
            SemanticTypeReferenceKind.Primitive => Primitive(reference.Primitive),
            SemanticTypeReferenceKind.Concept => CommonType(context.Concepts[reference.Target].Name),
            SemanticTypeReferenceKind.CompositeType => CommonType(context.Types[reference.Target].Name),
            _ => throw UnsupportedSemanticRendering.For(nameof(SemanticTypeReferenceKind), reference.Kind)
        };

        // Only reducer-visible records require concrete immutable inputs. Other artifacts keep their
        // existing collection contracts; switching an unrelated projection or command changes semantics.
        var type = scalar;
        if (reference.IsCollection)
        {
            type = reducerInput ? $"global::System.Collections.Immutable.ImmutableArray<{scalar}>" : $"global::System.Collections.Generic.IReadOnlyList<{scalar}>";
        }

        return reference.IsOptional ? $"{type}?" : type;
    }

    /// <summary>
    /// Whether the rendered type already carries Chronicle's event-source identity.
    /// </summary>
    /// <param name="type">The semantic type reference.</param>
    /// <returns>Whether the generated type derives from an event-source identity.</returns>
    public bool IsEventSourceIdentifier(SemanticTypeReference type) =>
        !type.IsCollection && type.Kind == SemanticTypeReferenceKind.Concept &&
        context.IdentifierConcepts.Contains(type.Target) && context.Concepts[type.Target].Values.IsEmpty;

    /// <summary>Whether a reducer identifier has an exact, supported Chronicle wire conversion.</summary>
    /// <param name="type">The read-model identifier type.</param>
    /// <returns>Whether its wire representation can be compared without normalization.</returns>
    public bool SupportsReducerIdentifier(SemanticTypeReference type)
    {
        if (type.IsOptional || type.IsCollection)
        {
            return false;
        }

        if (type.Kind == SemanticTypeReferenceKind.Primitive)
        {
            return type.Primitive is SemanticPrimitiveType.Uuid or SemanticPrimitiveType.Text;
        }

        if (type.Kind != SemanticTypeReferenceKind.Concept || !context.IdentifierConcepts.Contains(type.Target) ||
            !context.Concepts.TryGetValue(type.Target, out var concept) || !concept.Values.IsEmpty)
        {
            return false;
        }

        return concept.Primitive is SemanticPrimitiveType.Uuid or SemanticPrimitiveType.Text;
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
            return concept.Values.IsEmpty
                ? $"new {CommonType(concept.Name)}({PrimitiveValue(value, concept.Primitive)})"
                : EnumMember(CommonType(concept.Name), concept, value);
        }

        if (type.Kind == SemanticTypeReferenceKind.CompositeType && value is SemanticCompositeValue composite)
        {
            var semanticType = context.Types[type.Target];
            var values = semanticType.Properties.Select(property =>
                Value(composite.Properties.Single(_ => _.TargetProperty == property.Id).Value, property.Type));
            return $"new {CommonType(semanticType.Name)}({string.Join(", ", values)})";
        }

        return PrimitiveValue(value, type.Primitive);
    }

    /// <summary>
    /// Converts an admitted required scalar destination using Arc's event-source value semantics.
    /// </summary>
    /// <param name="expression">The command property or concrete specification value expression.</param>
    /// <param name="type">The destination type.</param>
    /// <returns>The EventSourceId-compatible expression.</returns>
    public string EventSourceExpression(string expression, SemanticTypeReference type)
    {
        var hasImplicitConversion = type.Kind == SemanticTypeReferenceKind.Primitive &&
            type.Primitive is SemanticPrimitiveType.Text or SemanticPrimitiveType.Uuid;
        var isIdentityConcept = type.Kind == SemanticTypeReferenceKind.Concept &&
            context.IdentifierConcepts.Contains(type.Target) && context.Concepts[type.Target].Values.IsEmpty;

        // ESM destinations are required scalars. Arc preserves typed identity conversion, otherwise calls
        // ToString() with the current culture. Parentheses also preserve negative numeric spec literals.
        return Expressions.EventSourceExpression.Render(expression, hasImplicitConversion || isIdentityConcept);
    }

    /// <summary>
    /// Gets a fully qualified generated concept or composite type name.
    /// </summary>
    /// <param name="name">The modeled type name.</param>
    /// <returns>The C# type name.</returns>
    public string CommonType(string name) => $"global::{context.CommonNamespace}.{Identifiers.ToPascalCase(name)}";

    /// <summary>
    /// Gets a fully qualified generated event type name.
    /// </summary>
    /// <param name="event">The modeled event.</param>
    /// <returns>The C# type name.</returns>
    public string EventType(SemanticEventContract @event) => SliceType(@event.Id, @event.Name);

    /// <summary>
    /// Gets a fully qualified generated slice declaration name.
    /// </summary>
    /// <param name="id">The declaration identity.</param>
    /// <param name="name">The modeled declaration name.</param>
    /// <returns>The C# type name.</returns>
    public string SliceType(SemanticId id, string name) =>
        $"global::{SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(id).Path)}.{Identifiers.ToPascalCase(name)}";

    // An enum concept renders as a C# enum, so its literal is a member rather than a wrapped primitive.
    static string EnumMember(string type, SemanticConcept concept, SemanticValue value) =>
        value is SemanticTextValue text && concept.Values.Contains(text.Value, StringComparer.Ordinal)
            ? $"{type}.{Identifiers.ToPascalCase(text.Value)}"
            : throw UnsupportedSemanticRendering.For($"{nameof(SemanticValueKind)}/enum {concept.Name}", value is SemanticTextValue unknown ? unknown.Value : value.Kind.ToString());

    static string Literal(string value) =>
        $"\"{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"";

    string PrimitiveValue(SemanticValue value, SemanticPrimitiveType primitive) => (value, primitive) switch
    {
        (SemanticTextValue text, SemanticPrimitiveType.Uuid) => $"global::System.Guid.Parse({Literal(text.Value)})",
        (SemanticTextValue text, SemanticPrimitiveType.Date) => $"global::System.DateOnly.Parse({Literal(text.Value)}, global::System.Globalization.CultureInfo.InvariantCulture)",
        (SemanticTextValue text, SemanticPrimitiveType.DateTime) => $"global::System.DateTimeOffset.Parse({Literal(text.Value)}, global::System.Globalization.CultureInfo.InvariantCulture)",
        (SemanticTextValue text, SemanticPrimitiveType.Text) => Literal(text.Value),
        (SemanticNumberValue number, SemanticPrimitiveType.DecimalNumber) => $"{number.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}m",
        (SemanticNumberValue number, SemanticPrimitiveType.WholeNumber) => number.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + (_usesLongWholeNumbers ? "L" : string.Empty),
        (SemanticBooleanValue boolean, SemanticPrimitiveType.Boolean) => boolean.Value ? "true" : "false",
        _ => throw UnsupportedSemanticRendering.For($"{nameof(SemanticValueKind)}/{nameof(SemanticPrimitiveType)}", $"{value.Kind}/{primitive}")
    };
}
