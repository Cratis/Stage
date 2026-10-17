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
    internal static bool CanRender(SemanticCondition condition, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context, bool numericOrderingOnly = false, bool dates = false) => condition switch
    {
        SemanticLogicalCondition logical => Enum.IsDefined(logical.Operator) &&
            CanRender(logical.Left, properties, context, numericOrderingOnly, dates) && CanRender(logical.Right, properties, context, numericOrderingOnly, dates),
        SemanticComparison comparison => Enum.IsDefined(comparison.Operator) &&
            CanRender(comparison.Left, properties, context) && CanRender(comparison.Right, properties, context) &&
            (DateComparison(comparison, properties, context) is not null ? dates || !numericOrderingOnly :
                CanCompareNamedValues(comparison, properties, context, numericOrderingOnly) &&
                (!numericOrderingOnly || comparison.Operator is SemanticComparisonOperator.Equal or SemanticComparisonOperator.NotEqual ||
                    (IsNumeric(comparison.Left, properties, context) && IsNumeric(comparison.Right, properties, context)))),
        _ => false
    };

    internal static string Render(SemanticCondition condition, SemanticConditionRenderingContext context) => condition switch
    {
        SemanticLogicalCondition logical => $"({Render(logical.Left, context)} {(logical.Operator == SemanticLogicalOperator.And ? "&&" : "||")} {Render(logical.Right, context)})",
        SemanticComparison comparison => Comparison(comparison, context),
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticCondition), condition.GetType().Name)
    };

    static bool CanRender(SemanticConditionOperand operand, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context)
    {
        if (!operand.Property.IsSet)
        {
            return operand.Value is SemanticNumberValue or SemanticBooleanValue ||
                (operand.Value is SemanticTextValue text && !text.Value.Any(_ => char.IsControl(_) || char.IsSurrogate(_)));
        }

        var property = properties.FirstOrDefault(_ => _.Id == operand.Property);
        return property?.Type is { IsCollection: false, IsOptional: false, Kind: not SemanticTypeReferenceKind.CompositeType } type &&
            (type.Kind != SemanticTypeReferenceKind.Concept ||
             context.Concepts.ContainsKey(type.Target));
    }

    static SemanticConcept? NamedConcept(SemanticConditionOperand operand, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context)
    {
        var type = properties.FirstOrDefault(property => property.Id == operand.Property)?.Type;
        return type?.Kind == SemanticTypeReferenceKind.Concept && context.Concepts.TryGetValue(type.Target, out var concept) && !concept.Values.IsEmpty ? concept : null;
    }

    static bool CanCompareNamedValues(SemanticComparison comparison, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context, bool numericOrderingOnly)
    {
        var left = NamedConcept(comparison.Left, properties, context);
        var right = NamedConcept(comparison.Right, properties, context);
        if (left is null && right is null) return true;
        if (numericOrderingOnly) return false;
        return comparison.Operator is SemanticComparisonOperator.Equal or SemanticComparisonOperator.NotEqual &&
            NamedOperandsMatch(comparison, left, right);
    }

    static bool NamedOperandsMatch(SemanticComparison comparison, SemanticConcept? left, SemanticConcept? right)
    {
        if (left is not null && right is not null) return left.Id == right.Id;
        return left is null ? IsNamedMember(comparison.Left, right!) : IsNamedMember(comparison.Right, left);
    }

    static bool IsNamedMember(SemanticConditionOperand operand, SemanticConcept concept) =>
        !operand.Property.IsSet && operand.Value is SemanticTextValue text && concept.Values.Contains(text.Value, StringComparer.Ordinal);

    static SemanticConcept? ComparisonConcept(SemanticComparison comparison, SemanticConditionRenderingContext context) =>
        NamedConcept(comparison.Left, context.Properties, context.Application) ?? NamedConcept(comparison.Right, context.Properties, context.Application);

    static string Comparison(SemanticComparison comparison, SemanticConditionRenderingContext context)
    {
        if (DateComparison(comparison, context.Properties, context.Application) is { } date)
        {
            // DateOnly and DateTimeOffset order calendar dates and UTC instants at tick precision, as the reference evaluator does.
            return $"({DateOperand(comparison.Left, date, context)}.CompareTo({DateOperand(comparison.Right, date, context)}) {ComparisonSymbol(comparison.Operator)} 0)";
        }

        var concept = ComparisonConcept(comparison, context);
        var left = ComparisonOperand(comparison.Left, concept, context);
        var right = ComparisonOperand(comparison.Right, concept, context);
        var symbol = ComparisonSymbol(comparison.Operator);

        return IsNumeric(comparison.Left, context.Properties, context.Application) ? $"({left} {symbol} {right})" :
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
        if (!comparison.Left.Property.IsSet || DateProperty(comparison.Left.Property, properties, context) is not { } primitive) return null;
        if (comparison.Right.Property.IsSet)
        {
            return DateProperty(comparison.Right.Property, properties, context) == primitive ? primitive : null;
        }

        return comparison.Right.Value is SemanticTextValue text && SemanticDateLiteral.IsCanonical(text.Value, primitive) ? primitive : null;
    }

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

    static string ComparisonOperand(SemanticConditionOperand operand, SemanticConcept? concept, SemanticConditionRenderingContext context) =>
        concept is not null && !operand.Property.IsSet
            ? new SemanticTypeSystem(context.Application).Value(operand.Value!, SemanticTypeReference.ForConcept(concept.Id))
            : Operand(operand, context);

    static bool IsNumeric(SemanticConditionOperand operand, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context) =>
        Primitive(operand, properties, context) is SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber;

    static SemanticPrimitiveType Primitive(SemanticConditionOperand operand, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context) =>
        operand.Property.IsSet
            ? SemanticValidationRendering.UnderlyingPrimitive(properties.Single(_ => _.Id == operand.Property).Type, context)
            : operand.Value switch
            {
                SemanticTextValue => SemanticPrimitiveType.Text,
                SemanticBooleanValue => SemanticPrimitiveType.Boolean,
                _ => SemanticPrimitiveType.DecimalNumber
            };

    static string Operand(SemanticConditionOperand operand, SemanticConditionRenderingContext context) => operand.Property.IsSet
        ? PropertyOperand(context.Properties.Single(_ => _.Id == operand.Property), context)
        : LiteralOperand(operand.Value);

    static string PropertyOperand(SemanticProperty property, SemanticConditionRenderingContext context)
    {
        var value = context.Accessor is null ? $"{context.Receiver}.{Identifiers.ToPascalCase(property.Name)}" : context.Accessor(property);

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
