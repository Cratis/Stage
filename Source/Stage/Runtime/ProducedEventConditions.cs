// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Stage.Contracts.Commands;

namespace Cratis.Stage.Runtime;

/// <summary>
/// Evaluates the condition guarding a produced event - the modeled <c>produces when</c> clause - against a command
/// payload.
/// </summary>
public static class ProducedEventConditions
{
    /// <summary>
    /// Determines whether a condition holds for a command payload. A condition that is absent always holds.
    /// </summary>
    /// <param name="condition">The condition to evaluate, or <see langword="null"/>.</param>
    /// <param name="command">The command payload to evaluate against.</param>
    /// <returns>True when the guarded event should be produced.</returns>
    public static bool Holds(ProducedEventCondition? condition, IReadOnlyDictionary<string, JsonElement> command) =>
        condition switch
        {
            null => true,
            ProducedEventComparison comparison => Compare(comparison, command),
            ProducedEventLogicalCondition { Operator: ProducedEventLogicalOperator.And } logical =>
                Holds(logical.Left, command) && Holds(logical.Right, command),
            ProducedEventLogicalCondition logical => Holds(logical.Left, command) || Holds(logical.Right, command),
            _ => true
        };

    static bool Compare(ProducedEventComparison comparison, IReadOnlyDictionary<string, JsonElement> command)
    {
        // A property the payload does not carry has no value to compare, so no comparison over it holds.
        if (CommandPayloadValues.Lookup(command, comparison.Property) is not { } element)
        {
            return false;
        }

        var expected = CommandPayloadValues.Parse(comparison.Value);

        if (comparison.Operator is ProducedEventComparisonOperator.Equal or ProducedEventComparisonOperator.NotEqual)
        {
            var equal = string.Equals(CommandPayloadValues.Text(element), expected?.ToString() ?? string.Empty, StringComparison.Ordinal);

            return comparison.Operator == ProducedEventComparisonOperator.Equal ? equal : !equal;
        }

        if (!TryNumber(element, out var actual) || expected is not JsonValue value || !value.TryGetValue<double>(out var limit))
        {
            return false;
        }

        return comparison.Operator switch
        {
            ProducedEventComparisonOperator.GreaterThan => actual > limit,
            ProducedEventComparisonOperator.GreaterThanOrEqual => actual >= limit,
            ProducedEventComparisonOperator.LessThan => actual < limit,
            ProducedEventComparisonOperator.LessThanOrEqual => actual <= limit,
            _ => false
        };
    }

    static bool TryNumber(JsonElement element, out double number)
    {
        if (element.ValueKind == JsonValueKind.Number)
        {
            return element.TryGetDouble(out number);
        }

        number = 0;

        return element.ValueKind == JsonValueKind.String && double.TryParse(element.GetString(), CultureInfo.InvariantCulture, out number);
    }
}
