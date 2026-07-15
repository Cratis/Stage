// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Stage.Validation;

/// <summary>
/// Helpers for reading typed values out of a raw command payload for rule evaluation.
/// </summary>
public static class JsonValues
{
    /// <summary>
    /// Reads the property as a string, or <see langword="null"/> when it is absent, JSON null, or a non-string value.
    /// A non-string, non-null value is rendered via its raw text so length/format rules still see something.
    /// </summary>
    /// <param name="values">The payload.</param>
    /// <param name="property">The property name.</param>
    /// <returns>The string value or <see langword="null"/>.</returns>
    public static string? GetString(this IReadOnlyDictionary<string, JsonElement> values, string property)
    {
        if (!values.TryGetValue(property, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Array => value.GetArrayLength() == 0 ? string.Empty : value.GetRawText(),
            _ => value.GetRawText(),
        };
    }

    /// <summary>
    /// Reads the property as a number, or <see langword="null"/> when it is absent or not numeric.
    /// </summary>
    /// <param name="values">The payload.</param>
    /// <param name="property">The property name.</param>
    /// <returns>The numeric value or <see langword="null"/>.</returns>
    public static double? GetNumber(this IReadOnlyDictionary<string, JsonElement> values, string property)
    {
        if (!values.TryGetValue(property, out var value) || value.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return value.TryGetDouble(out var number) ? number : null;
    }

    /// <summary>
    /// Parses a JSON object string into a dictionary of property values, returning an empty dictionary when the
    /// string is missing or not a JSON object.
    /// </summary>
    /// <param name="json">The JSON object string.</param>
    /// <returns>The parsed values.</returns>
    public static IReadOnlyDictionary<string, JsonElement> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, JsonElement>();
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new Dictionary<string, JsonElement>();
            }

            return document.RootElement.EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.Clone());
        }
        catch (JsonException)
        {
            return new Dictionary<string, JsonElement>();
        }
    }
}
