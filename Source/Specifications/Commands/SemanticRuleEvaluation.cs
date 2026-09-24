// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Specifications.Comparison;

namespace Cratis.Stage.Specifications.Commands;

internal static class SemanticRuleEvaluation
{
    internal static string? FirstFailure(SemanticCommand command, SemanticSpecification specification, IEnumerable<SemanticConcept> concepts)
    {
        var values = specification.When!.Values.ToDictionary(value => value.TargetProperty, value => value.Value);
        foreach (var rule in command.Validations)
        {
            if (!Satisfies(rule, values[rule.Property]))
            {
                return rule.Message ?? Message(rule, values[rule.Property], "A required value is empty.");
            }
        }

        var byId = concepts.ToDictionary(concept => concept.Id);
        foreach (var property in command.Properties.Where(property => property.Type.Kind == SemanticTypeReferenceKind.Concept))
        {
            foreach (var rule in byId[property.Type.Target].Validations)
            {
                if (!Satisfies(rule, values[property.Id]))
                {
                    return rule.Message ?? Message(rule, values[property.Id], "A required concept value is empty.");
                }
            }
        }

        foreach (var requirement in command.Requirements)
        {
            if (!Condition(requirement.Condition, values))
            {
                return requirement.Message ?? "Command requirement was not met.";
            }
        }

        return null;
    }

    internal static bool Condition(SemanticCondition condition, IReadOnlyDictionary<SemanticId, SemanticValue> values) => condition switch
    {
        SemanticLogicalCondition { Operator: SemanticLogicalOperator.And } logical => Condition(logical.Left, values) && Condition(logical.Right, values),
        SemanticLogicalCondition { Operator: SemanticLogicalOperator.Or } logical => Condition(logical.Left, values) || Condition(logical.Right, values),
        SemanticComparison comparison => Compare(comparison, values),
        _ => throw new UnsupportedSemanticMapping()
    };

    static bool Compare(SemanticComparison comparison, IReadOnlyDictionary<SemanticId, SemanticValue> values)
    {
        var left = comparison.Left.Property.IsSet ? values[comparison.Left.Property] : comparison.Left.Value!;
        var right = comparison.Right.Property.IsSet ? values[comparison.Right.Property] : comparison.Right.Value!;
        if (comparison.Operator == SemanticComparisonOperator.Equal) return SemanticExpectationComparer.AreEqual(left, right);
        if (comparison.Operator == SemanticComparisonOperator.NotEqual) return !SemanticExpectationComparer.AreEqual(left, right);
        if (left is not SemanticNumberValue a || right is not SemanticNumberValue b) throw new UnsupportedSemanticMapping();
        return comparison.Operator switch
        {
            SemanticComparisonOperator.GreaterThan => a.Value > b.Value,
            SemanticComparisonOperator.GreaterThanOrEqual => a.Value >= b.Value,
            SemanticComparisonOperator.LessThan => a.Value < b.Value,
            SemanticComparisonOperator.LessThanOrEqual => a.Value <= b.Value,
            _ => throw new UnsupportedSemanticMapping()
        };
    }

    static bool Satisfies(SemanticValidationRule rule, SemanticValue value) => rule.Kind switch
    {
        SemanticValidationRuleKind.NotEmpty => !SemanticRunContext.Empty(value),
        _ when value is SemanticNullValue => true,
        SemanticValidationRuleKind.Maximum => Measure(value) <= Number(rule.Operand),
        SemanticValidationRuleKind.Minimum => Measure(value) >= Number(rule.Operand),
        SemanticValidationRuleKind.Equal => SemanticExpectationComparer.AreEqual(value, rule.Operand!),
        SemanticValidationRuleKind.NotEqual => !SemanticExpectationComparer.AreEqual(value, rule.Operand!),
        SemanticValidationRuleKind.GreaterThan => Measure(value) > Number(rule.Operand),
        SemanticValidationRuleKind.GreaterThanOrEqual => Measure(value) >= Number(rule.Operand),
        SemanticValidationRuleKind.LessThan => Measure(value) < Number(rule.Operand),
        SemanticValidationRuleKind.LessThanOrEqual => Measure(value) <= Number(rule.Operand),
        SemanticValidationRuleKind.Length => value is SemanticTextValue text && text.Value.Length == Number(rule.Operand),
        SemanticValidationRuleKind.Matches => Matches(value, rule.Operand),
        _ => throw new UnsupportedSemanticMapping()
    };

    static bool Matches(SemanticValue value, SemanticValue? operand)
    {
        if (value is not SemanticTextValue text || operand is not SemanticTextValue pattern) throw new UnsupportedSemanticMapping();
        try
        {
            return Regex.IsMatch(text.Value, pattern.Value, RegexOptions.ECMAScript | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    static decimal Measure(SemanticValue value) => value switch
    {
        SemanticTextValue text => text.Value.Length,
        SemanticNumberValue number => number.Value,
        _ => throw new UnsupportedSemanticMapping()
    };

    static decimal Number(SemanticValue? value) => value is SemanticNumberValue number ? number.Value : throw new UnsupportedSemanticMapping();

    static string Format(SemanticValue? value) => value switch
    {
        SemanticNumberValue number => number.Value.ToString(CultureInfo.InvariantCulture),
        SemanticTextValue text => $"'{text.Value}'",
        SemanticBooleanValue boolean => boolean.Value ? "true" : "false",
        _ => "null"
    };

    static string Message(SemanticValidationRule rule, SemanticValue value, string empty) => rule.Kind switch
    {
        SemanticValidationRuleKind.NotEmpty => empty,
        SemanticValidationRuleKind.Maximum when value is SemanticTextValue => $"A value must be at most {Format(rule.Operand)} characters long.",
        SemanticValidationRuleKind.Minimum when value is SemanticTextValue => $"A value must be at least {Format(rule.Operand)} characters long.",
        SemanticValidationRuleKind.Maximum => $"A value must be at most {Format(rule.Operand)}.",
        SemanticValidationRuleKind.Minimum => $"A value must be at least {Format(rule.Operand)}.",
        SemanticValidationRuleKind.Equal => $"A value must equal {Format(rule.Operand)}.",
        SemanticValidationRuleKind.NotEqual => $"A value must not equal {Format(rule.Operand)}.",
        SemanticValidationRuleKind.GreaterThan => $"A value must be greater than {Format(rule.Operand)}.",
        SemanticValidationRuleKind.GreaterThanOrEqual => $"A value must be at least {Format(rule.Operand)}.",
        SemanticValidationRuleKind.LessThan => $"A value must be less than {Format(rule.Operand)}.",
        SemanticValidationRuleKind.LessThanOrEqual => $"A value must be at most {Format(rule.Operand)}.",
        SemanticValidationRuleKind.Length => $"A value must be exactly {Format(rule.Operand)} characters long.",
        SemanticValidationRuleKind.Matches => "A value must match the required pattern.",
        _ => throw new UnsupportedSemanticMapping()
    };
}
