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
            .Where(produced => ProducedEventConditions.Holds(produced.When, command))
            .Select(produced => new ProducedEventPayload(
                produced.Event,
                Payload(produced, command, occurred, identity),
                produced.Tags))
    ];

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
            ProducedValueKind.CommandProperty => CommandPayloadValues.Lookup(command, property.Expression) is { } element
                ? JsonNode.Parse(element.GetRawText())
                : null,
            ProducedValueKind.Literal => CommandPayloadValues.Parse(property.Expression),
            ProducedValueKind.Occurred => JsonValue.Create(occurred.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)),
            ProducedValueKind.Identity => identity.TryGetValue(property.Expression, out var value) ? JsonValue.Create(value) : null,
            ProducedValueKind.Environment => Environment.GetEnvironmentVariable(property.Expression) is { } variable ? JsonValue.Create(variable) : null,
            ProducedValueKind.Template => JsonValue.Create(Interpolate(property.Expression, command)),
            _ => null
        };

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
                .Append(CommandPayloadValues.Text(CommandPayloadValues.Lookup(command, template[(start + 2)..end])));
            position = end + 1;
        }

        return builder.ToString();
    }
}
