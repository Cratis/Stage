// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Specifications.Commands;

/// <summary>
/// Evaluates the UTC now comparison subset against an explicitly supplied command occurrence.
/// </summary>
internal static class SemanticUtcNow
{
    internal const string MissingOccurrence = "A command using a clock operand needs the instant of the command; the execution request supplies no occurrence.";

    static readonly Dictionary<SemanticComparisonOperator, Func<int, bool>> _comparisons = new()
    {
        [SemanticComparisonOperator.Equal] = order => order == 0,
        [SemanticComparisonOperator.NotEqual] = order => order != 0,
        [SemanticComparisonOperator.GreaterThan] = order => order > 0,
        [SemanticComparisonOperator.GreaterThanOrEqual] = order => order >= 0,
        [SemanticComparisonOperator.LessThan] = order => order < 0,
        [SemanticComparisonOperator.LessThanOrEqual] = order => order <= 0
    };

    internal static bool Uses(SemanticComparison comparison) =>
        comparison.Left.Clock is not null || comparison.Right.Clock is not null || comparison.Left.DateOf || comparison.Right.DateOf;

    internal static bool Compare(SemanticComparison comparison, IReadOnlyDictionary<SemanticId, SemanticValue> values, DateTimeOffset? occurrence) =>
        Satisfies(comparison.Operator, Operand(comparison.Left, values, occurrence).CompareTo(Operand(comparison.Right, values, occurrence)));

    internal static DateTimeOffset Instant(SemanticClockOperand operand, DateTimeOffset? occurrence)
    {
        if (operand.Kind != SemanticClockKind.Now || operand.DateOf) throw new UnsupportedSemanticMapping();
        var instant = occurrence ?? throw new InvalidSemanticContract(MissingOccurrence);
        return Offset(operand, instant);
    }

    static DateTimeOffset Offset(SemanticClockOperand operand, DateTimeOffset instant) => operand.Unit switch
        {
            SemanticClockUnit.Day => instant.AddHours(24d * operand.Amount),
            SemanticClockUnit.Hour => instant.AddHours(operand.Amount),
            SemanticClockUnit.Minute => instant.AddMinutes(operand.Amount),
            _ => throw new UnsupportedSemanticMapping()
        };

    static DateTimeOffset Operand(SemanticConditionOperand operand, IReadOnlyDictionary<SemanticId, SemanticValue> values, DateTimeOffset? occurrence)
    {
        if (operand.DateOf || operand.Read is not null) throw new UnsupportedSemanticMapping();
        return operand.Clock is { } clock ? Instant(clock, occurrence) : Parse(values[operand.Property]);
    }

    static DateTimeOffset Parse(SemanticValue value) =>
        value is SemanticTextValue text && SemanticDateComparisons.TryParseInstant(text.Value, out var instant)
            ? instant
            : throw new InvalidSemanticContract("A date comparison requires valid dates or explicitly offset instants.");

    static bool Satisfies(SemanticComparisonOperator comparison, int order) =>
        _comparisons.TryGetValue(comparison, out var predicate) ? predicate(order) : throw new UnsupportedSemanticMapping();

}
