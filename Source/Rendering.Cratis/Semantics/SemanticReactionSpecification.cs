// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// The single-hop reaction selected by a concrete append fixture.
/// </summary>
/// <param name="Reaction">The reactor whose handler receives the fixture.</param>
/// <param name="Trigger">The selected event trigger.</param>
/// <param name="Fires">Whether the fixture satisfies the modeled condition.</param>
internal sealed record SemanticReactionSpecification(SemanticReaction Reaction, SemanticReactionTrigger Trigger, bool Fires)
{
    internal static IEnumerable<SemanticReactionSpecification> Candidates(SemanticApplicationContext context, SemanticSpecificationAppend append) =>
        SemanticVersionFeatures.Slices(context.Application).SelectMany(slice => slice.Reactions.IsDefault ? [] : slice.Reactions)
            .SelectMany(reaction => reaction.Triggers.Where(trigger => trigger.Kind == SemanticReactionTriggerKind.Event && trigger.Source == append.EventContract)
                .Select(trigger => new SemanticReactionSpecification(reaction, trigger, trigger.Where is null || Matches(trigger.Where, append))));

    internal SemanticSpecification Lower(SemanticSpecification specification, SemanticApplicationContext context)
    {
        var invocation = Trigger.Invokes.Single();
        var command = context.Commands[invocation.Command];
        var values = command.Properties.Where(property => !property.IsGenerated).Select(property => new SemanticPropertyValue(
            property.Id,
            invocation.Mappings.SingleOrDefault(mapping => mapping.TargetProperty == property.Id) is { } mapping
                ? Evaluate(mapping.Source, specification.WhenAppended!) : SemanticValue.Null));

        // A reaction never forwards the fixture caller. The generated scenario establishes identity
        // from the reactor attribute, not from this lowered command fixture.
        return specification with { When = new(command.Id, [.. values]), WhenAppended = null, GivenCaller = null };
    }

    internal static SemanticValue Evaluate(SemanticExpression expression, SemanticSpecificationAppend append) => expression switch
    {
        SemanticValueExpression literal => literal.Value,
        SemanticResolvedExpression { Root: SemanticExpressionRootKind.Event, Source: SemanticExpressionSourceKind.Property } property =>
            append.Values.Single(value => value.TargetProperty == property.Target).Value,
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticExpression), expression.Kind)
    };

    static bool Matches(SemanticCondition condition, SemanticSpecificationAppend append) => condition switch
    {
        SemanticLogicalCondition { Operator: SemanticLogicalOperator.And } logical => Matches(logical.Left, append) && Matches(logical.Right, append),
        SemanticLogicalCondition { Operator: SemanticLogicalOperator.Or } logical => Matches(logical.Left, append) || Matches(logical.Right, append),
        SemanticComparison comparison => Compare(comparison, append),
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticCondition), condition.GetType().Name)
    };

    static bool Compare(SemanticComparison comparison, SemanticSpecificationAppend append)
    {
        var left = Operand(comparison.Left, append);
        var right = Operand(comparison.Right, append);

        return comparison.Operator switch
        {
            SemanticComparisonOperator.Equal => Equal(left, right),
            SemanticComparisonOperator.NotEqual => !Equal(left, right),
            _ => CompareNumbers(comparison.Operator, (SemanticNumberValue)left, (SemanticNumberValue)right)
        };
    }

    static bool CompareNumbers(SemanticComparisonOperator operation, SemanticNumberValue left, SemanticNumberValue right) => operation switch
    {
        SemanticComparisonOperator.GreaterThan => left.Value > right.Value,
        SemanticComparisonOperator.GreaterThanOrEqual => left.Value >= right.Value,
        SemanticComparisonOperator.LessThan => left.Value < right.Value,
        SemanticComparisonOperator.LessThanOrEqual => left.Value <= right.Value,
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticComparisonOperator), operation)
    };

    static bool Equal(SemanticValue left, SemanticValue right) => (left, right) switch
    {
        (SemanticArrayValue a, SemanticArrayValue b) => a.Values.Length == b.Values.Length && a.Values.Zip(b.Values).All(pair => Equal(pair.First, pair.Second)),
        (SemanticCompositeValue a, SemanticCompositeValue b) => a.Properties.Length == b.Properties.Length && a.Properties.All(property => b.Properties.Any(other => other.TargetProperty == property.TargetProperty && Equal(property.Value, other.Value))),
        _ => Equals(left, right)
    };

    static SemanticValue Operand(SemanticConditionOperand operand, SemanticSpecificationAppend append) => operand.Property.IsSet
        ? append.Values.Single(value => value.TargetProperty == operand.Property).Value : operand.Value!;
}
