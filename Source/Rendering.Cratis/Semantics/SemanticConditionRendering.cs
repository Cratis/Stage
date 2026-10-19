// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders portable conditions over command or triggering-event properties.
/// </summary>
internal static class SemanticConditionRendering
{
    internal static bool CanRender(SemanticCondition condition, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context, bool numericOrderingOnly = false, bool dates = false, SemanticReadScope? reads = null) => condition switch
    {
        SemanticLogicalCondition logical => Enum.IsDefined(logical.Operator) &&
            CanRender(logical.Left, properties, context, numericOrderingOnly, dates, reads) && CanRender(logical.Right, properties, context, numericOrderingOnly, dates, reads),
        SemanticComparison comparison => Enum.IsDefined(comparison.Operator) &&
            CanRender(comparison.Left, properties, context, reads) && CanRender(comparison.Right, properties, context, reads) &&
            !ComparesReadDate(comparison, properties, context, reads) &&
            CanCompareUuids(comparison, properties, context, reads) &&
            (DateComparison(comparison, properties, context) is not null ? dates || !numericOrderingOnly :
                CanCompareNamedValues(comparison, properties, context, numericOrderingOnly, reads) &&
                (!numericOrderingOnly || comparison.Operator is SemanticComparisonOperator.Equal or SemanticComparisonOperator.NotEqual ||
                    (IsNumeric(comparison.Left, properties, context, reads) && IsNumeric(comparison.Right, properties, context, reads)))),
        _ => false
    };

    internal static string Render(SemanticCondition condition, SemanticConditionRenderingContext context) => condition switch
    {
        SemanticLogicalCondition logical => $"({Render(logical.Left, context)} {(logical.Operator == SemanticLogicalOperator.And ? "&&" : "||")} {Render(logical.Right, context)})",
        SemanticComparison comparison => Comparison(comparison, context),
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticCondition), condition.GetType().Name)
    };

    static SemanticProperty? Resolve(SemanticConditionOperand operand, IReadOnlyList<SemanticProperty> properties, SemanticReadScope? reads) =>
        operand.Read is { } read ? reads?.Property(read, operand.Property) : properties.FirstOrDefault(_ => _.Id == operand.Property);

    static bool CanRender(SemanticConditionOperand operand, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context, SemanticReadScope? reads)
    {
        if (!operand.Property.IsSet)
        {
            return operand.Value is SemanticNumberValue or SemanticBooleanValue ||
                (operand.Value is SemanticTextValue text && !text.Value.Any(_ => char.IsControl(_) || char.IsSurrogate(_)));
        }

        var property = Resolve(operand, properties, reads);
        return property?.Type is { IsCollection: false, IsOptional: false, Kind: not SemanticTypeReferenceKind.CompositeType } type &&
            (type.Kind != SemanticTypeReferenceKind.Concept ||
             context.Concepts.ContainsKey(type.Target));
    }

    static SemanticConcept? NamedConcept(SemanticConditionOperand operand, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context, SemanticReadScope? reads)
    {
        var type = Resolve(operand, properties, reads)?.Type;
        return type?.Kind == SemanticTypeReferenceKind.Concept && context.Concepts.TryGetValue(type.Target, out var concept) && !concept.Values.IsEmpty ? concept : null;
    }

    static bool CanCompareNamedValues(SemanticComparison comparison, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context, bool numericOrderingOnly, SemanticReadScope? reads)
    {
        var left = NamedConcept(comparison.Left, properties, context, reads);
        var right = NamedConcept(comparison.Right, properties, context, reads);
        if (left is null && right is null) return true;
        if (numericOrderingOnly) return false;
        return comparison.Operator is SemanticComparisonOperator.Equal or SemanticComparisonOperator.NotEqual &&
            NamedOperandsMatch(comparison, left, right);
    }

    static bool CanCompareUuids(SemanticComparison comparison, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context, SemanticReadScope? reads)
    {
        var left = IsUuid(comparison.Left, properties, context, reads);
        var right = IsUuid(comparison.Right, properties, context, reads);
        if (!left && !right) return true;

        return (left || IsCanonicalUuidLiteral(comparison.Left)) && (right || IsCanonicalUuidLiteral(comparison.Right));
    }

    static bool IsCanonicalUuidLiteral(SemanticConditionOperand operand) =>
        !operand.Property.IsSet && operand.Value is SemanticTextValue text &&
        Guid.TryParseExact(text.Value, "D", out var guid) && string.Equals(guid.ToString("D", CultureInfo.InvariantCulture), text.Value, StringComparison.Ordinal);

    static bool NamedOperandsMatch(SemanticComparison comparison, SemanticConcept? left, SemanticConcept? right)
    {
        if (left is not null && right is not null) return left.Id == right.Id;
        return left is null ? IsNamedMember(comparison.Left, right!) : IsNamedMember(comparison.Right, left);
    }

    static bool IsNamedMember(SemanticConditionOperand operand, SemanticConcept concept) =>
        !operand.Property.IsSet && operand.Value is SemanticTextValue text && concept.Values.Contains(text.Value, StringComparer.Ordinal);

    static SemanticConcept? ComparisonConcept(SemanticComparison comparison, SemanticConditionRenderingContext context) =>
        NamedConcept(comparison.Left, context.Properties, context.Application, context.Reads) ?? NamedConcept(comparison.Right, context.Properties, context.Application, context.Reads);

    static string Comparison(SemanticComparison comparison, SemanticConditionRenderingContext context)
    {
        if (DateComparison(comparison, context.Properties, context.Application) is { } date)
        {
            // DateOnly and DateTimeOffset order calendar dates and UTC instants at tick precision, as the reference evaluator does.
            return $"({DateOperand(comparison.Left, date, context)}.CompareTo({DateOperand(comparison.Right, date, context)}) {ComparisonSymbol(comparison.Operator)} 0)";
        }

        var concept = ComparisonConcept(comparison, context);
        var uuid = IsUuid(comparison.Left, context.Properties, context.Application, context.Reads) || IsUuid(comparison.Right, context.Properties, context.Application, context.Reads);
        var left = ComparisonOperand(comparison.Left, concept, uuid, context);
        var right = ComparisonOperand(comparison.Right, concept, uuid, context);
        var symbol = ComparisonSymbol(comparison.Operator);

        return IsNumeric(comparison.Left, context.Properties, context.Application, context.Reads) ? $"({left} {symbol} {right})" :
            $"({(comparison.Operator == SemanticComparisonOperator.NotEqual ? "!" : string.Empty)}global::System.Object.Equals({left}, {right}))";
    }

    /// <summary>
    /// Finds the Date or DateTime primitive of a comparison the reference evaluator orders chronologically.
    /// </summary>
    /// <remarks>
    /// The left operand is a required scalar command property of a Date or DateTime primitive, directly or through a concept.
    /// The right operand is a required scalar property of the same primitive or a canonical text literal.
    /// </remarks>
    /// <param name="comparison">The comparison.</param>
    /// <param name="properties">The command properties.</param>
    /// <param name="context">The indexed application.</param>
    /// <returns>The shared Date or DateTime primitive, or null when the comparison is not chronological.</returns>
    static SemanticPrimitiveType? DateComparison(SemanticComparison comparison, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context)
    {
        if (comparison.Left.Read is not null || comparison.Right.Read is not null) return null;
        if (!comparison.Left.Property.IsSet || DateProperty(comparison.Left.Property, properties, context) is not { } primitive) return null;
        if (comparison.Right.Property.IsSet)
        {
            return DateProperty(comparison.Right.Property, properties, context) == primitive ? primitive : null;
        }

        return comparison.Right.Value is SemanticTextValue text && SemanticDateLiteral.IsCanonical(text.Value, primitive) ? primitive : null;
    }

    // A protected read is not a command input; ordering its dates is not rendered, so such a comparison stays refused.
    static bool ComparesReadDate(SemanticComparison comparison, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context, SemanticReadScope? reads) =>
        (comparison.Left.Read is not null || comparison.Right.Read is not null) &&
        new[] { comparison.Left, comparison.Right }.Any(operand => operand.Property.IsSet &&
            Primitive(operand, properties, context, reads) is SemanticPrimitiveType.Date or SemanticPrimitiveType.DateTime);

    static SemanticPrimitiveType? DateProperty(SemanticId id, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context)
    {
        var type = properties.FirstOrDefault(property => property.Id == id)?.Type;
        if (type is null or { IsCollection: true } or { IsOptional: true } or { Kind: SemanticTypeReferenceKind.CompositeType }) return null;
        if (type.Kind == SemanticTypeReferenceKind.Concept && (!context.Concepts.TryGetValue(type.Target, out var concept) || !concept.Values.IsEmpty)) return null;
        var primitive = SemanticValidationRendering.UnderlyingPrimitive(type, context);

        return primitive is SemanticPrimitiveType.Date or SemanticPrimitiveType.DateTime ? primitive : null;
    }

    static string DateOperand(SemanticConditionOperand operand, SemanticPrimitiveType primitive, SemanticConditionRenderingContext context)
    {
        if (operand.Property.IsSet) return Operand(operand, context);

        return SemanticDateLiteral.Render(((SemanticTextValue)operand.Value!).Value, primitive, type => $"global::System.{type}");
    }

    static string ComparisonSymbol(SemanticComparisonOperator comparison) => comparison switch
    {
        SemanticComparisonOperator.Equal => "==",
        SemanticComparisonOperator.NotEqual => "!=",
        SemanticComparisonOperator.GreaterThan => ">",
        SemanticComparisonOperator.GreaterThanOrEqual => ">=",
        SemanticComparisonOperator.LessThan => "<",
        SemanticComparisonOperator.LessThanOrEqual => "<=",
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticComparisonOperator), comparison)
    };

    static string ComparisonOperand(SemanticConditionOperand operand, SemanticConcept? concept, bool uuid, SemanticConditionRenderingContext context)
    {
        if (operand.Property.IsSet) return Operand(operand, context);
        if (concept is not null) return new SemanticTypeSystem(context.Application).Value(operand.Value!, SemanticTypeReference.ForConcept(concept.Id));

        return uuid
            ? new SemanticTypeSystem(context.Application).Value(operand.Value!, SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Uuid))
            : Operand(operand, context);
    }

    static bool IsUuid(SemanticConditionOperand operand, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context, SemanticReadScope? reads) =>
        operand.Property.IsSet && Primitive(operand, properties, context, reads) == SemanticPrimitiveType.Uuid;

    static bool IsNumeric(SemanticConditionOperand operand, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context, SemanticReadScope? reads) =>
        Primitive(operand, properties, context, reads) is SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber;

    static SemanticPrimitiveType Primitive(SemanticConditionOperand operand, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context, SemanticReadScope? reads) =>
        operand.Property.IsSet
            ? SemanticValidationRendering.UnderlyingPrimitive(Resolve(operand, properties, reads)!.Type, context)
            : operand.Value switch
            {
                SemanticTextValue => SemanticPrimitiveType.Text,
                SemanticBooleanValue => SemanticPrimitiveType.Boolean,
                _ => SemanticPrimitiveType.DecimalNumber
            };

    static string Operand(SemanticConditionOperand operand, SemanticConditionRenderingContext context)
    {
        if (!operand.Property.IsSet) return LiteralOperand(operand.Value);

        return operand.Read is { } read
            ? ReadOperand(read, operand.Property, context)
            : PropertyOperand(context.Properties.Single(_ => _.Id == operand.Property), context);
    }

    static string ReadOperand(string read, SemanticId id, SemanticConditionRenderingContext context)
    {
        var scope = context.Reads ?? throw UnsupportedSemanticRendering.For(nameof(SemanticConditionOperand.Read), read);
        var property = scope.Property(read, id) ?? throw UnsupportedSemanticRendering.For(nameof(SemanticConditionOperand.Read), read);
        var value = $"{scope.Local(read)}.{Identifiers.ToPascalCase(property.Name)}";

        return property.Type.Kind == SemanticTypeReferenceKind.Concept && context.Application.Concepts[property.Type.Target].Values.IsEmpty ? $"{value}.Value" : value;
    }

    static string PropertyOperand(SemanticProperty property, SemanticConditionRenderingContext context)
    {
        var value = context.Accessor is null ? $"{context.Receiver}.{context.Application.MemberNames.Property(property)}" : context.Accessor(property);

        return property.Type.Kind == SemanticTypeReferenceKind.Concept && context.Raw?.Contains(property.Id) != true && context.Application.Concepts[property.Type.Target].Values.IsEmpty ? $"{value}.Value" : value;
    }

    static string LiteralOperand(SemanticValue? value) => value switch
    {
        SemanticTextValue text => CSharpCodeBuilder.StringLiteral(text.Value),
        SemanticBooleanValue boolean => boolean.Value ? "true" : "false",
        SemanticNumberValue number => number.Value.ToString(CultureInfo.InvariantCulture) + "m",
        null => throw UnsupportedSemanticRendering.For(nameof(SemanticValue), "null"),
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticValue), value.GetType().Name)
    };
}
