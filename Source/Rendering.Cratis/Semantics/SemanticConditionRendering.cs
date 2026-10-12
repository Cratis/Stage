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
    internal static bool CanRender(SemanticCondition condition, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context, bool numericOrderingOnly = false) => condition switch
    {
        SemanticLogicalCondition logical => Enum.IsDefined(logical.Operator) &&
            CanRender(logical.Left, properties, context, numericOrderingOnly) && CanRender(logical.Right, properties, context, numericOrderingOnly),
        SemanticComparison comparison => Enum.IsDefined(comparison.Operator) &&
            CanRender(comparison.Left, properties, context) && CanRender(comparison.Right, properties, context) &&
            (!numericOrderingOnly || comparison.Operator is SemanticComparisonOperator.Equal or SemanticComparisonOperator.NotEqual ||
                (IsNumeric(comparison.Left, properties, context) && IsNumeric(comparison.Right, properties, context))),
        _ => false
    };

    internal static string Render(SemanticCondition condition, IReadOnlyList<SemanticProperty> properties, string receiver, SemanticApplicationContext context) => condition switch
    {
        SemanticLogicalCondition logical => $"({Render(logical.Left, properties, receiver, context)} {(logical.Operator == SemanticLogicalOperator.And ? "&&" : "||")} {Render(logical.Right, properties, receiver, context)})",
        SemanticComparison comparison => Comparison(comparison, properties, receiver, context),
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
             (context.Concepts.TryGetValue(type.Target, out var concept) && concept.Values.IsEmpty));
    }

    static string Comparison(SemanticComparison comparison, IReadOnlyList<SemanticProperty> properties, string receiver, SemanticApplicationContext context)
    {
        var left = Operand(comparison.Left, properties, receiver, context);
        var right = Operand(comparison.Right, properties, receiver, context);
        var symbol = comparison.Operator switch
        {
            SemanticComparisonOperator.Equal => "==",
            SemanticComparisonOperator.NotEqual => "!=",
            SemanticComparisonOperator.GreaterThan => ">",
            SemanticComparisonOperator.GreaterThanOrEqual => ">=",
            SemanticComparisonOperator.LessThan => "<",
            SemanticComparisonOperator.LessThanOrEqual => "<=",
            _ => throw UnsupportedSemanticRendering.For(nameof(SemanticComparisonOperator), comparison.Operator)
        };

        return IsNumeric(comparison.Left, properties, context) ? $"({left} {symbol} {right})" :
            $"({(comparison.Operator == SemanticComparisonOperator.NotEqual ? "!" : string.Empty)}global::System.Object.Equals({left}, {right}))";
    }

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

    static string Operand(SemanticConditionOperand operand, IReadOnlyList<SemanticProperty> properties, string receiver, SemanticApplicationContext context)
    {
        if (operand.Property.IsSet)
        {
            var property = properties.Single(_ => _.Id == operand.Property);
            var value = $"{receiver}.{Identifiers.ToPascalCase(property.Name)}";
            return property.Type.Kind == SemanticTypeReferenceKind.Concept ? $"{value}.Value" : value;
        }

        return operand.Value switch
        {
            SemanticTextValue text => CSharpCodeBuilder.StringLiteral(text.Value),
            SemanticBooleanValue boolean => boolean.Value ? "true" : "false",
            SemanticNumberValue number => number.Value.ToString(CultureInfo.InvariantCulture) + "m",
            _ => throw UnsupportedSemanticRendering.For(nameof(SemanticValue), operand.Value?.GetType().Name ?? "null")
        };
    }
}
