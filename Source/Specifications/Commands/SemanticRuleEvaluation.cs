// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Specifications.Comparison;

namespace Cratis.Stage.Specifications.Commands;

internal static class SemanticRuleEvaluation
{
    internal static string? FirstFailure(SemanticCommand command, SemanticSpecification specification, IEnumerable<SemanticConcept> concepts, IEnumerable<SemanticCompositeType>? composites = null)
        => Failures(command, specification, concepts, composites).FirstOrDefault()?.Message;

    internal static IEnumerable<SemanticValidationFailure> Failures(SemanticCommand command, SemanticSpecification specification, IEnumerable<SemanticConcept> concepts, IEnumerable<SemanticCompositeType>? composites = null) =>
        RuleFailures(command, specification, concepts, composites).Concat(RequirementFailures(command, specification));

    internal static IEnumerable<SemanticValidationFailure> RuleFailures(SemanticCommand command, SemanticSpecification specification, IEnumerable<SemanticConcept> concepts, IEnumerable<SemanticCompositeType>? composites = null)
    {
        var values = specification.When!.Values.ToDictionary(value => value.TargetProperty, value => value.Value);
        foreach (var rule in command.Validations)
        {
            if (!Satisfies(rule, values[rule.Property]))
            {
                yield return new(rule.Message ?? Message(rule, values[rule.Property], "A required value is empty."), rule.Severity);
            }
        }

        foreach (var failure in ConceptFailures(command, values, concepts, composites))
        {
            yield return failure;
        }
    }

    internal static IEnumerable<SemanticValidationFailure> RequirementFailures(SemanticCommand command, SemanticSpecification specification, IReadOnlyDictionary<string, IReadOnlyDictionary<SemanticId, SemanticValue>>? reads = null)
    {
        var values = specification.When!.Values.ToDictionary(value => value.TargetProperty, value => value.Value);
        foreach (var requirement in command.Requirements)
        {
            if (!Condition(requirement.Condition, values, reads))
            {
                yield return new(requirement.Message ?? "Command requirement was not met.", requirement.Severity);
            }
        }
    }

    internal static bool Condition(SemanticCondition condition, IReadOnlyDictionary<SemanticId, SemanticValue> values, IReadOnlyDictionary<string, IReadOnlyDictionary<SemanticId, SemanticValue>>? reads = null) => condition switch
    {
        SemanticLogicalCondition { Operator: SemanticLogicalOperator.And } logical => Condition(logical.Left, values, reads) && Condition(logical.Right, values, reads),
        SemanticLogicalCondition { Operator: SemanticLogicalOperator.Or } logical => Condition(logical.Left, values, reads) || Condition(logical.Right, values, reads),
        SemanticComparison comparison => Compare(comparison, values, reads),
        _ => throw new UnsupportedSemanticMapping()
    };

    internal static bool Satisfies(SemanticValidationRule rule, SemanticValue value) => SemanticValidationRules.Check(rule, value).Status switch
    {
        SemanticRuleStatus.Satisfied => true,
        SemanticRuleStatus.Failed => false,
        _ => throw new UnsupportedSemanticMapping()
    };

    internal static string Message(SemanticValidationRule rule, SemanticValue value, string empty) =>
        rule.Kind == SemanticValidationRuleKind.NotEmpty ? rule.Message ?? empty : SemanticValidationRules.Check(rule, value).Message;

    static IEnumerable<SemanticValidationFailure> ConceptFailures(SemanticCommand command, Dictionary<SemanticId, SemanticValue> values, IEnumerable<SemanticConcept> concepts, IEnumerable<SemanticCompositeType>? composites)
    {
        var byId = concepts.ToDictionary(concept => concept.Id);
        var types = (composites ?? []).ToDictionary(composite => composite.Id);

        return command.Properties.Where(property => !property.IsGenerated)
            .SelectMany(property => SemanticConceptValues.Validate(property.Type, values[property.Id], byId, types));
    }

    static bool Compare(SemanticComparison comparison, IReadOnlyDictionary<SemanticId, SemanticValue> values, IReadOnlyDictionary<string, IReadOnlyDictionary<SemanticId, SemanticValue>>? reads)
    {
        var left = Operand(comparison.Left, values, reads);
        var right = Operand(comparison.Right, values, reads);
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

    static SemanticValue Operand(SemanticConditionOperand operand, IReadOnlyDictionary<SemanticId, SemanticValue> values, IReadOnlyDictionary<string, IReadOnlyDictionary<SemanticId, SemanticValue>>? reads) => operand switch
    {
        { Read: { } read } => reads![read].GetValueOrDefault(operand.Property, SemanticValue.Null),
        { Property.IsSet: true } => values[operand.Property],
        _ => operand.Value!
    };

}
