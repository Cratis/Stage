// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Stage.Contracts.Commands;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Converts a Screenplay <see cref="ConditionSyntax"/> into the Stage <see cref="ProducedEventCondition"/> tree.
/// </summary>
/// <remarks>
/// One condition converter for every construct that carries a condition — a <c>produces when</c> guard and a
/// <c>require</c> rule are the same condition grammar in the language, so they are the same tree here. A second
/// tree for requirements would let <c>and</c> and <c>or</c> drift apart between the two.
/// </remarks>
public static class ConditionConverter
{
    /// <summary>
    /// Converts a condition into its Stage tree.
    /// </summary>
    /// <param name="condition">The condition to convert, or <see langword="null"/> when none is declared.</param>
    /// <returns>The Stage condition, or <see langword="null"/> when there is none to express.</returns>
    public static ProducedEventCondition? Convert(ConditionSyntax? condition) =>
        condition switch
        {
            null => null,
            ComparisonConditionSyntax comparison => new ProducedEventComparison(
                comparison.Left,
                Operator(comparison.Operator),
                ProducedValueConverter.Convert(comparison.Right) is { Kind: ProducedValueKind.Literal } literal ? literal.Expression : "null"),
            LogicalConditionSyntax logical when Convert(logical.Left) is { } left && Convert(logical.Right) is { } right =>
                new ProducedEventLogicalCondition(left, Operator(logical.Operator), right),
            _ => null
        };

    /// <summary>
    /// Converts a Screenplay logical operator into its Stage equivalent.
    /// </summary>
    /// <param name="operator">The operator to convert.</param>
    /// <returns>The Stage operator.</returns>
    public static ProducedEventLogicalOperator Operator(LogicalOperator @operator) =>
        @operator == LogicalOperator.Or ? ProducedEventLogicalOperator.Or : ProducedEventLogicalOperator.And;

    static ProducedEventComparisonOperator Operator(ComparisonOperator @operator) =>
        @operator switch
        {
            ComparisonOperator.Equal => ProducedEventComparisonOperator.Equal,
            ComparisonOperator.NotEqual => ProducedEventComparisonOperator.NotEqual,
            ComparisonOperator.GreaterThan => ProducedEventComparisonOperator.GreaterThan,
            ComparisonOperator.GreaterThanOrEqual => ProducedEventComparisonOperator.GreaterThanOrEqual,
            ComparisonOperator.LessThan => ProducedEventComparisonOperator.LessThan,
            ComparisonOperator.LessThanOrEqual => ProducedEventComparisonOperator.LessThanOrEqual,
            _ => ProducedEventComparisonOperator.Equal
        };
}
