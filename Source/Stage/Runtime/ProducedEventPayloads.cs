// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Stage.Contracts.Commands;

namespace Cratis.Stage.Runtime;

/// <summary>
/// Evaluates a command's modeled <c>produces</c> declarations against a command payload, yielding the event payloads
/// to append. Purely a function of the payload and the model — no IO, so it is directly specifiable.
/// </summary>
public static class ProducedEventPayloads
{
    /// <summary>
    /// Builds the payload for every event the command produces for the given input.
    /// </summary>
    /// <param name="produces">The modeled produced events, in declaration order.</param>
    /// <param name="command">The command payload the request bound into.</param>
    /// <param name="occurred">The time to use for properties sourced from the occurred time.</param>
    /// <param name="identity">The identity that caused the command, used for identity-sourced properties.</param>
    /// <returns>One <see cref="ProducedEventPayload"/> per event whose condition holds, in declaration order.</returns>
    public static IReadOnlyList<ProducedEventPayload> Build(
        IReadOnlyList<ProducedEvent> produces,
        IReadOnlyDictionary<string, JsonElement> command,
        DateTimeOffset occurred,
        IReadOnlyDictionary<string, string> identity) =>
    [
        .. produces
            .Where(produced => Holds(produced.When, command))
            .Select(produced => new ProducedEventPayload(
                produced.Event,
                Payload(produced, command, occurred, identity),
                produced.Tags))
    ];

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

    static JsonObject Payload(
        ProducedEvent produced,
        IReadOnlyDictionary<string, JsonElement> command,
        DateTimeOffset occurred,
        IReadOnlyDictionary<string, string> identity)
    {
        var payload = new JsonObject();

        foreach (var property in produced.Properties)
        {
            if (Value(property, command, occurred, identity) is { } value)
            {
                payload[property.Property] = value;
            }
        }

        return payload;
    }

    static JsonNode? Value(
        ProducedEventProperty property,
        IReadOnlyDictionary<string, JsonElement> command,
        DateTimeOffset occurred,
        IReadOnlyDictionary<string, string> identity) =>
        property.Kind switch
        {
            ProducedValueKind.CommandProperty => Lookup(command, property.Expression) is { } element ? JsonNode.Parse(element.GetRawText()) : null,
            ProducedValueKind.Literal => Parse(property.Expression),
            ProducedValueKind.Occurred => JsonValue.Create(occurred.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)),
            ProducedValueKind.Identity => identity.TryGetValue(property.Expression, out var value) ? JsonValue.Create(value) : null,
            ProducedValueKind.Environment => Environment.GetEnvironmentVariable(property.Expression) is { } variable ? JsonValue.Create(variable) : null,
            ProducedValueKind.Template => JsonValue.Create(Interpolate(property.Expression, command)),
            _ => null
        };

    static JsonNode? Parse(string json)
    {
        try
        {
            return JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            // A literal the model could not render as JSON is not a runtime failure - the property is simply
            // left off the payload, the same as any other unresolvable source.
            return null;
        }
    }

    static string Interpolate(string template, IReadOnlyDictionary<string, JsonElement> command)
    {
        var builder = new StringBuilder();
        var position = 0;

        while (position < template.Length)
        {
            var start = template.IndexOf("${", position, StringComparison.Ordinal);
            if (start < 0)
            {
                builder.Append(template, position, template.Length - position);
                break;
            }

            var end = template.IndexOf('}', start);
            if (end < 0)
            {
                builder.Append(template, position, template.Length - position);
                break;
            }

            builder
                .Append(template, position, start - position)
                .Append(Text(Lookup(command, template[(start + 2)..end])));
            position = end + 1;
        }

        return builder.ToString();
    }

    static bool Compare(ProducedEventComparison comparison, IReadOnlyDictionary<string, JsonElement> command)
    {
        if (Lookup(command, comparison.Property) is not { } element)
        {
            return false;
        }

        var expected = Parse(comparison.Value);

        if (comparison.Operator is ProducedEventComparisonOperator.Equal or ProducedEventComparisonOperator.NotEqual)
        {
            var equal = string.Equals(Text(element), expected?.ToString() ?? string.Empty, StringComparison.Ordinal);
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

    static JsonElement? Lookup(IReadOnlyDictionary<string, JsonElement> command, string property)
    {
        if (command.TryGetValue(property, out var element))
        {
            return element;
        }

        // The payload is bound straight from the request body, so its casing is whatever the caller sent.
        foreach (var candidate in command)
        {
            if (string.Equals(candidate.Key, property, StringComparison.OrdinalIgnoreCase))
            {
                return candidate.Value;
            }
        }

        return null;
    }

    static string Text(JsonElement? element) =>
        element switch
        {
            null => string.Empty,
            { ValueKind: JsonValueKind.String } value => value.GetString() ?? string.Empty,
            { } value => value.GetRawText()
        };
}
