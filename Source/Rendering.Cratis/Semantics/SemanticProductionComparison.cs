// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Compares portable scalar production operands for rendering and runtime admission.
/// </summary>
internal static class SemanticProductionComparison
{
    static readonly Dictionary<SemanticComparisonOperator, Func<decimal, decimal, bool>> _ordering =
        new Dictionary<SemanticComparisonOperator, Func<decimal, decimal, bool>>
        {
            [SemanticComparisonOperator.GreaterThan] = (left, right) => left > right,
            [SemanticComparisonOperator.GreaterThanOrEqual] = (left, right) => left >= right,
            [SemanticComparisonOperator.LessThan] = (left, right) => left < right,
            [SemanticComparisonOperator.LessThanOrEqual] = (left, right) => left <= right
        };

    internal static bool Compare(SemanticComparison comparison, IReadOnlyDictionary<SemanticId, SemanticValue> values, (Func<Exception> Nonnumeric, Func<Exception> Unknown) errors)
    {
        var left = Operand(comparison.Left, values);
        var right = Operand(comparison.Right, values);

        return comparison.Operator switch
        {
            SemanticComparisonOperator.Equal => Equals(left, right),
            SemanticComparisonOperator.NotEqual => !Equals(left, right),
            _ => CompareNumbers(comparison.Operator, left, right, errors)
        };
    }

    static SemanticValue? Operand(SemanticConditionOperand operand, IReadOnlyDictionary<SemanticId, SemanticValue> values) =>
        operand.Property.IsSet ? values[operand.Property] : operand.Value;

    static bool CompareNumbers(SemanticComparisonOperator comparison, SemanticValue? left, SemanticValue? right, (Func<Exception> Nonnumeric, Func<Exception> Unknown) errors)
    {
        if (left is not SemanticNumberValue a || right is not SemanticNumberValue b) throw errors.Nonnumeric();
        if (!_ordering.TryGetValue(comparison, out var compare)) throw errors.Unknown();

        return compare(a.Value, b.Value);
    }
}
