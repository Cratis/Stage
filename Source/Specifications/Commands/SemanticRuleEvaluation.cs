// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Specifications.Comparison;

namespace Cratis.Stage.Specifications.Commands;

internal static class SemanticRuleEvaluation
{
    internal static string? FirstFailure(SemanticCommand command, SemanticSpecification specification, IEnumerable<SemanticConcept> concepts)
        => Failures(command, specification, concepts).FirstOrDefault()?.Message;

    internal static IEnumerable<SemanticValidationFailure> Failures(SemanticCommand command, SemanticSpecification specification, IEnumerable<SemanticConcept> concepts)
    {
        var values = specification.When!.Values.ToDictionary(value => value.TargetProperty, value => value.Value);
        foreach (var rule in command.Validations)
        {
            if (!Satisfies(rule, values[rule.Property]))
            {
                yield return new(rule.Message ?? Message(rule, values[rule.Property], "A required value is empty."), rule.Severity);
            }
        }

        var byId = concepts.ToDictionary(concept => concept.Id);
        foreach (var property in command.Properties.Where(IsInputConcept))
        {
            foreach (var rule in byId[property.Type.Target].Validations)
            {
                if (!Satisfies(rule, values[property.Id]))
                {
                    yield return new(rule.Message ?? Message(rule, values[property.Id], "A required concept value is empty."), rule.Severity);
                }
            }
        }

        foreach (var requirement in command.Requirements)
        {
            if (!Condition(requirement.Condition, values))
            {
                yield return new(requirement.Message ?? "Command requirement was not met.", requirement.Severity);
            }
        }
    }

    internal static bool Condition(SemanticCondition condition, IReadOnlyDictionary<SemanticId, SemanticValue> values) => condition switch
    {
        SemanticLogicalCondition { Operator: SemanticLogicalOperator.And } logical => Condition(logical.Left, values) && Condition(logical.Right, values),
        SemanticLogicalCondition { Operator: SemanticLogicalOperator.Or } logical => Condition(logical.Left, values) || Condition(logical.Right, values),
        SemanticComparison comparison => Compare(comparison, values),
        _ => throw new UnsupportedSemanticMapping()
    };

    internal static bool Satisfies(SemanticValidationRule rule, SemanticValue value) => SemanticRuleValues.Satisfies(rule, value, () => new UnsupportedSemanticMapping());

    internal static string Message(SemanticValidationRule rule, SemanticValue value, string empty) => rule.Kind switch
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
        SemanticValidationRuleKind.AllGreaterThan => $"Every value must be greater than {Format(rule.Operand)}.",
        SemanticValidationRuleKind.AllGreaterThanOrEqual => $"Every value must be at least {Format(rule.Operand)}.",
        _ => throw new UnsupportedSemanticMapping()
    };

    static bool IsInputConcept(SemanticProperty property) => !property.IsGenerated && property.Type.Kind == SemanticTypeReferenceKind.Concept;

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

    static string Format(SemanticValue? value) => value switch
    {
        SemanticNumberValue number => number.Value.ToString(CultureInfo.InvariantCulture),
        SemanticTextValue text => $"'{text.Value}'",
        SemanticBooleanValue boolean => boolean.Value ? "true" : "false",
        _ => "null"
    };
}
