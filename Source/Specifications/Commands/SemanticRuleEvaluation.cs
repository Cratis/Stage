// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Specifications.Comparison;

namespace Cratis.Stage.Specifications.Commands;

internal static class SemanticRuleEvaluation
{
    internal static string? FirstFailure(SemanticCommand command, SemanticSpecification specification, SemanticApplication application)
        => Failures(command, specification, application).FirstOrDefault()?.Message;

    internal static IEnumerable<SemanticValidationFailure> Failures(SemanticCommand command, SemanticSpecification specification, SemanticApplication application) =>
        RuleFailures(command, specification, application).Concat(RequirementFailures(command, specification, dateTypes: SemanticDateComparisons.Participants(application, command)));

    internal static IEnumerable<SemanticValidationFailure> RuleFailures(SemanticCommand command, SemanticSpecification specification, SemanticApplication application) =>
        RuleFailures(command, specification, application, null);

    internal static IEnumerable<SemanticValidationFailure> RuleFailures(SemanticCommand command, SemanticSpecification specification, SemanticApplication application, SemanticRunContext? run)
    {
        var values = specification.When!.Values.ToDictionary(value => value.TargetProperty, value => value.Value);
        var dateTypes = SemanticDateComparisons.Participants(application, command);
        foreach (var rule in command.Validations)
        {
            // Opaque predicates always use their worker, including properties in chronological comparisons.
            var check = rule.Kind != SemanticValidationRuleKind.RulePredicate && dateTypes.TryGetValue(rule.Property, out var primitive)
                ? SemanticDateComparisons.Check(rule, values[rule.Property], primitive)
                : CommandCheck(rule, values[rule.Property], run);
            if (!Satisfied(check))
            {
                yield return new(rule.Message ?? (rule.Kind == SemanticValidationRuleKind.NotEmpty ? "A required value is empty." : check.Message), rule.Severity);
            }
        }

        foreach (var failure in ConceptFailures(command, values, application, run))
        {
            yield return failure;
        }
    }

    internal static IEnumerable<SemanticValidationFailure> RequirementFailures(SemanticCommand command, SemanticSpecification specification, IReadOnlyDictionary<string, IReadOnlyDictionary<SemanticId, SemanticValue>>? reads = null, IReadOnlyDictionary<SemanticId, SemanticPrimitiveType>? dateTypes = null)
    {
        var values = specification.When!.Values.ToDictionary(value => value.TargetProperty, value => value.Value);
        foreach (var requirement in command.Requirements)
        {
            if (!Condition(requirement.Condition, values, reads, dateTypes))
            {
                yield return new(requirement.Message ?? "Command requirement was not met.", requirement.Severity);
            }
        }
    }

    internal static bool Condition(SemanticCondition condition, IReadOnlyDictionary<SemanticId, SemanticValue> values, IReadOnlyDictionary<string, IReadOnlyDictionary<SemanticId, SemanticValue>>? reads = null, IReadOnlyDictionary<SemanticId, SemanticPrimitiveType>? dateTypes = null) => condition switch
    {
        SemanticLogicalCondition { Operator: SemanticLogicalOperator.And } logical => Condition(logical.Left, values, reads, dateTypes) && Condition(logical.Right, values, reads, dateTypes),
        SemanticLogicalCondition { Operator: SemanticLogicalOperator.Or } logical => Condition(logical.Left, values, reads, dateTypes) || Condition(logical.Right, values, reads, dateTypes),
        SemanticComparison comparison => Compare(comparison, values, reads, dateTypes),
        _ => throw new UnsupportedSemanticMapping()
    };

    internal static bool Satisfies(SemanticValidationRule rule, SemanticValue value) => Satisfied(SemanticValidationRules.Check(rule, value));

    internal static string Message(SemanticValidationRule rule, SemanticValue value, string empty) =>
        rule.Kind == SemanticValidationRuleKind.NotEmpty ? rule.Message ?? empty : SemanticValidationRules.Check(rule, value).Message;

    internal static SemanticRuleCheck ConceptCheck(SemanticValidationRule rule, SemanticValue value, SemanticRunContext? run) =>
        rule.Kind == SemanticValidationRuleKind.RulePredicate && run?.BodyRules is { } bodies
            ? bodies.Check(rule, new(value, value, string.Empty, run.Tenant, null, run.AuthoredOccurrence), run.BodyCancellation)
            : SemanticValidationRules.Check(rule, value);

    internal static bool Satisfies(SemanticRuleCheck check) => Satisfied(check);

    static bool Satisfied(SemanticRuleCheck check) => check.Status switch
    {
        SemanticRuleStatus.Satisfied => true,
        SemanticRuleStatus.Failed => false,
        _ => throw Unsupported(check)
    };

    static UnsupportedSemanticMapping Unsupported(SemanticRuleCheck check) =>
        check.Message.StartsWith("STAGE-", StringComparison.Ordinal) ? new(check.Message) : new();

    static SemanticRuleCheck CommandCheck(SemanticValidationRule rule, SemanticValue value, SemanticRunContext? run)
    {
        if (rule.Kind != SemanticValidationRuleKind.RulePredicate || run?.BodyRules is not { } bodies) return SemanticValidationRules.Check(rule, value);
        var artifact = SemanticValue.Composite(run.Specification.When!.Values);
        var property = run.Command.Properties.First(member => member.Id == rule.Property).Name;
        return bodies.Check(rule, new(artifact, value, property, run.Tenant, null, run.AuthoredOccurrence), run.BodyCancellation);
    }

    static IEnumerable<SemanticValidationFailure> ConceptFailures(SemanticCommand command, Dictionary<SemanticId, SemanticValue> values, SemanticApplication application, SemanticRunContext? run)
    {
        var byId = application.Concepts.ToDictionary(concept => concept.Id);
        var types = application.Types.ToDictionary(composite => composite.Id);
        return command.Properties.Where(property => !property.IsGenerated)
            .SelectMany(property => run is null
                ? SemanticConceptValues.Validate(property.Type, values[property.Id], byId, types)
                : SemanticConceptValues.Validate(property.Type, values[property.Id], run));
    }

    static bool Compare(SemanticComparison comparison, IReadOnlyDictionary<SemanticId, SemanticValue> values, IReadOnlyDictionary<string, IReadOnlyDictionary<SemanticId, SemanticValue>>? reads, IReadOnlyDictionary<SemanticId, SemanticPrimitiveType>? dateTypes)
    {
        if (dateTypes?.ContainsKey(comparison.Left.Property) == true)
        {
            return SemanticConditionEvaluator.Evaluate(comparison, values, dateTypes, reads);
        }

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
