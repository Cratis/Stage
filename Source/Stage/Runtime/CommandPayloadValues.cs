// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Stage.Runtime;

/// <summary>
/// Reads values out of the payload a command bound from the request body, which has no compile-time shape.
/// </summary>
public static class CommandPayloadValues
{
    /// <summary>
    /// Looks up a property on the payload.
    /// </summary>
    /// <param name="command">The command payload.</param>
    /// <param name="property">The name of the property to look up.</param>
    /// <returns>The value, or <see langword="null"/> when the payload carries no such property.</returns>
    /// <remarks>
    /// The payload is bound straight from the request body, so its casing is whatever the caller sent - the exact
    /// name is tried first, then a case-insensitive match.
    /// </remarks>
    public static JsonElement? Lookup(IReadOnlyDictionary<string, JsonElement> command, string property)
    {
        if (command.TryGetValue(property, out var element))
        {
            return element;
        }

        foreach (var candidate in command)
        {
            if (string.Equals(candidate.Key, property, StringComparison.OrdinalIgnoreCase))
            {
                return candidate.Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Renders a value as text, unwrapping a JSON string rather than keeping its quotes.
    /// </summary>
    /// <param name="element">The value to render.</param>
    /// <returns>The text, or an empty string when there is no value.</returns>
    public static string Text(JsonElement? element) =>
        element switch
        {
            null => string.Empty,
            { ValueKind: JsonValueKind.String } value => value.GetString() ?? string.Empty,
            { } value => value.GetRawText()
        };

    /// <summary>
    /// Parses JSON text into a node.
    /// </summary>
    /// <param name="json">The JSON text to parse.</param>
    /// <returns>The parsed node, or <see langword="null"/> when the text is not valid JSON.</returns>
    /// <remarks>
    /// A constant the model could not render as JSON is not a runtime failure - it resolves to no value, which
    /// leaves the property off the payload the same as any other unresolvable source.
    /// </remarks>
    public static JsonNode? Parse(string json)
    {
        try
        {
            return JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
