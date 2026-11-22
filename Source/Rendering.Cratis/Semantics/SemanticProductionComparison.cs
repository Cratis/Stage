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
        new()
        {
            [SemanticComparisonOperator.GreaterThan] = (left, right) => left > right,
            [SemanticComparisonOperator.GreaterThanOrEqual] = (left, right) => left >= right,
            [SemanticComparisonOperator.LessThan] = (left, right) => left < right,
            [SemanticComparisonOperator.LessThanOrEqual] = (left, right) => left <= right
        };

    internal static bool Compare(SemanticComparison comparison, IReadOnlyDictionary<SemanticId, SemanticValue> values, (Func<Exception> Nonnumeric, Func<Exception> Unknown) errors, IReadOnlyDictionary<SemanticId, SemanticPrimitiveType>? dateTypes = null, DateTimeOffset? clock = null)
    {
        if (comparison.Left.Clock is not null || comparison.Right.Clock is not null)
        {
            return CompareValues(comparison.Operator, ClockValue(comparison.Left, values, clock, errors.Unknown), ClockValue(comparison.Right, values, clock, errors.Unknown), errors);
        }
        if (dateTypes?.ContainsKey(comparison.Left.Property) == true)
        {
            return SemanticConditionEvaluator.Evaluate(comparison, values, dateTypes);
        }

        var left = Operand(comparison.Left, values);
        var right = Operand(comparison.Right, values);

        return CompareValues(comparison.Operator, left, right, errors);
    }

    static bool CompareValues(SemanticComparisonOperator operation, SemanticValue? left, SemanticValue? right, (Func<Exception> Nonnumeric, Func<Exception> Unknown) errors) =>
        operation switch
        {
            SemanticComparisonOperator.Equal => Equals(left, right),
            SemanticComparisonOperator.NotEqual => !Equals(left, right),
            _ => CompareNumbers(operation, left, right, errors)
        };

    static SemanticValue ClockValue(SemanticConditionOperand operand, IReadOnlyDictionary<SemanticId, SemanticValue> values, DateTimeOffset? occurrence, Func<Exception> error)
    {
        if (operand.Read is not null || operand.DateOf) throw error();
        var instant = operand.Clock is { } clock ? ClockInstant(clock, occurrence ?? throw error(), error) : ParseInstant(Operand(operand, values), error);
        return SemanticValue.Number(instant.UtcTicks);
    }

    static DateTimeOffset ClockInstant(SemanticClockOperand clock, DateTimeOffset occurrence, Func<Exception> error)
    {
        if (clock.Kind != SemanticClockKind.Now || clock.DateOf) throw error();
        return clock.Unit switch
        {
            SemanticClockUnit.Day => occurrence.AddHours(24d * clock.Amount),
            SemanticClockUnit.Hour => occurrence.AddHours(clock.Amount),
            SemanticClockUnit.Minute => occurrence.AddMinutes(clock.Amount),
            _ => throw error()
        };
    }

    static DateTimeOffset ParseInstant(SemanticValue? value, Func<Exception> error) =>
        value is SemanticTextValue text && SemanticDateComparisons.TryParseInstant(text.Value, out var instant) ? instant : throw error();

    static SemanticValue? Operand(SemanticConditionOperand operand, IReadOnlyDictionary<SemanticId, SemanticValue> values) =>
        operand.Property.IsSet ? values[operand.Property] : operand.Value;

    static bool CompareNumbers(SemanticComparisonOperator comparison, SemanticValue? left, SemanticValue? right, (Func<Exception> Nonnumeric, Func<Exception> Unknown) errors)
    {
        if (left is not SemanticNumberValue a || right is not SemanticNumberValue b) throw errors.Nonnumeric();
        if (!_ordering.TryGetValue(comparison, out var compare)) throw errors.Unknown();

        return compare(a.Value, b.Value);
    }
}
