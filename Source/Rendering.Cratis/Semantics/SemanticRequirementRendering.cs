// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders command-property requirements as command-wide validation failures.
/// </summary>
internal static class SemanticRequirementRendering
{
    internal static bool CanRender(SemanticRequirement requirement, SemanticCommand command, SemanticApplicationContext context) =>
        SemanticValidationRendering.SafeMessage(requirement.Message) && requirement.Severity == SemanticValidationSeverity.Error &&
        CanRender(requirement.Condition, command, context);

    internal static void Render(CSharpCodeBuilder builder, SemanticRequirement requirement, SemanticCommand command, SemanticApplicationContext context)
    {
        var condition = Condition(requirement.Condition, command, context);
        var message = requirement.Message ?? "Command requirement was not met.";
        builder.Line($"RuleFor(_ => _).Must(command => {condition}).WithMessage({CSharpCodeBuilder.StringLiteral(message)});");
    }

    static bool CanRender(SemanticCondition condition, SemanticCommand command, SemanticApplicationContext context) => condition switch
    {
        SemanticLogicalCondition logical => Enum.IsDefined(logical.Operator) &&
            CanRender(logical.Left, command, context) && CanRender(logical.Right, command, context),
        SemanticComparison comparison => Enum.IsDefined(comparison.Operator) &&
            CanRender(comparison.Left, command, context) && CanRender(comparison.Right, command, context),
        _ => false
    };

    static bool CanRender(SemanticConditionOperand operand, SemanticCommand command, SemanticApplicationContext context)
    {
        if (!operand.Property.IsSet)
        {
            return operand.Value is SemanticNumberValue or SemanticBooleanValue ||
                (operand.Value is SemanticTextValue text && !text.Value.Any(_ => char.IsControl(_) || char.IsSurrogate(_)));
        }

        var property = command.Properties.FirstOrDefault(_ => _.Id == operand.Property);
        return property?.Type is { IsCollection: false, IsOptional: false, Kind: not SemanticTypeReferenceKind.CompositeType } type &&
            (type.Kind != SemanticTypeReferenceKind.Concept ||
             (context.Concepts.TryGetValue(type.Target, out var concept) && concept.Values.IsEmpty));
    }

    static string Condition(SemanticCondition condition, SemanticCommand command, SemanticApplicationContext context) => condition switch
    {
        SemanticLogicalCondition logical => $"({Condition(logical.Left, command, context)} {(logical.Operator == SemanticLogicalOperator.And ? "&&" : "||")} {Condition(logical.Right, command, context)})",
        SemanticComparison comparison => Comparison(comparison, command, context),
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticCondition), condition.GetType().Name)
    };

    static string Comparison(SemanticComparison comparison, SemanticCommand command, SemanticApplicationContext context)
    {
        var left = Operand(comparison.Left, command, context);
        var right = Operand(comparison.Right, command, context);

        // Equality of numeric values in ESM uses decimal equality even when one property is a whole number.
        // Non-numeric equality uses ordinal string equality and value equality for booleans.
        var numeric = Primitive(comparison.Left, command, context) is SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber;
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
        return numeric ? $"({left} {symbol} {right})" :
            $"({(comparison.Operator == SemanticComparisonOperator.NotEqual ? "!" : string.Empty)}object.Equals({left}, {right}))";
    }

    static SemanticPrimitiveType Primitive(SemanticConditionOperand operand, SemanticCommand command, SemanticApplicationContext context) =>
        operand.Property.IsSet
            ? SemanticValidationRendering.UnderlyingPrimitive(command.Properties.Single(_ => _.Id == operand.Property).Type, context)
            : operand.Value switch
            {
                SemanticTextValue => SemanticPrimitiveType.Text,
                SemanticBooleanValue => SemanticPrimitiveType.Boolean,
                _ => SemanticPrimitiveType.DecimalNumber
            };

    static string Operand(SemanticConditionOperand operand, SemanticCommand command, SemanticApplicationContext context)
    {
        if (operand.Property.IsSet)
        {
            var property = command.Properties.Single(_ => _.Id == operand.Property);
            var value = $"command.{Identifiers.ToPascalCase(property.Name)}";
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
