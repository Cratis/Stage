// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Selects command productions with the reference evaluator's scalar condition semantics.
/// </summary>
internal static class SemanticProductionSelection
{
    internal static ImmutableArray<SemanticProducedEvent> Taken(SemanticCommand command, SemanticSpecification specification) =>
        Taken(command, specification.When!.Values.Concat(specification.When.GeneratedValues));

    internal static ImmutableArray<SemanticProducedEvent> Taken(SemanticCommand command, IEnumerable<SemanticPropertyValue> values)
    {
        if (command.Produces.All(produced => produced.When is null)) return command.Produces;
        var inputs = values.ToDictionary(value => value.TargetProperty, value => value.Value);
        return [.. command.Produces.Where(produced => produced.When is null || Holds(produced.When, inputs))];
    }

    internal static IEnumerable<SemanticId> Properties(SemanticCondition condition) => condition switch
    {
        SemanticLogicalCondition logical => Properties(logical.Left).Concat(Properties(logical.Right)),
        SemanticComparison comparison => new[] { comparison.Left.Property, comparison.Right.Property }.Where(property => property.IsSet),
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticCondition), condition.GetType().Name)
    };

    static bool Holds(SemanticCondition condition, IReadOnlyDictionary<SemanticId, SemanticValue> values) => condition switch
    {
        SemanticLogicalCondition { Operator: SemanticLogicalOperator.And } logical => Holds(logical.Left, values) && Holds(logical.Right, values),
        SemanticLogicalCondition { Operator: SemanticLogicalOperator.Or } logical => Holds(logical.Left, values) || Holds(logical.Right, values),
        SemanticComparison comparison => Compare(comparison, values),
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticCondition), condition.GetType().Name)
    };

    static bool Compare(SemanticComparison comparison, IReadOnlyDictionary<SemanticId, SemanticValue> values)
    {
        var left = comparison.Left.Property.IsSet ? values[comparison.Left.Property] : comparison.Left.Value;
        var right = comparison.Right.Property.IsSet ? values[comparison.Right.Property] : comparison.Right.Value;
        if (comparison.Operator == SemanticComparisonOperator.Equal) return Equals(left, right);
        if (comparison.Operator == SemanticComparisonOperator.NotEqual) return !Equals(left, right);
        if (left is not SemanticNumberValue a || right is not SemanticNumberValue b) throw UnsupportedSemanticRendering.For(nameof(SemanticComparison), comparison.Operator);
        return comparison.Operator switch
        {
            SemanticComparisonOperator.GreaterThan => a.Value > b.Value,
            SemanticComparisonOperator.GreaterThanOrEqual => a.Value >= b.Value,
            SemanticComparisonOperator.LessThan => a.Value < b.Value,
            SemanticComparisonOperator.LessThanOrEqual => a.Value <= b.Value,
            _ => throw UnsupportedSemanticRendering.For(nameof(SemanticComparisonOperator), comparison.Operator)
        };
    }
}
