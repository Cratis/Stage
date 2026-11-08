// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Stage.Contracts.Screenplay;

namespace Cratis.Stage.Api;

/// <summary>
/// Keeps values of properties typed as a <c language="csharp">secret</c> concept out of what a command sends back to the caller,
/// reading the marker from the command's schema.
/// </summary>
internal static class StageSecretValues
{
    // A concept declared with the sensitive marker is the secret of the schema form, whatever else it is marked with.
    const string SecretMarker = "sensitive";

    /// <summary>
    /// Omits every secret-typed property, at any depth, from a command payload that is echoed back.
    /// </summary>
    /// <param name="payload">The payload as the caller sent it.</param>
    /// <param name="schema">The command's JSON schema.</param>
    /// <returns>The payload itself when nothing in it is secret; otherwise a copy without the secret values.</returns>
    internal static IDictionary<string, JsonElement> Payload(IDictionary<string, JsonElement> payload, string schema)
    {
        if (string.IsNullOrWhiteSpace(schema))
        {
            return payload;
        }

        try
        {
            using var document = JsonDocument.Parse(schema);
            return Carries(document.RootElement)
                ? Members(payload.Select(member => (member.Key, member.Value)), document.RootElement)
                : payload;
        }
        catch (JsonException)
        {
            // A schema that cannot be read cannot say what is secret, so nothing is echoed.
            return new Dictionary<string, JsonElement>();
        }
    }

    /// <summary>
    /// Gets whether a top-level property of the command is typed as a secret concept, so that nothing derived from its
    /// value - the value itself or its length - may reach a message.
    /// </summary>
    /// <param name="schema">The command's JSON schema.</param>
    /// <param name="property">The property name.</param>
    /// <returns>True when the property is secret.</returns>
    internal static bool IsSecretProperty(string schema, string property)
    {
        if (string.IsNullOrWhiteSpace(schema))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(schema);
            var node = document.RootElement;
            foreach (var segment in property.Split('.'))
            {
                var next = node.ValueKind == JsonValueKind.Object && node.TryGetProperty("properties", out var properties) && properties.ValueKind == JsonValueKind.Object
                    ? properties.EnumerateObject().Where(candidate => string.Equals(candidate.Name, segment, StringComparison.OrdinalIgnoreCase)).Select(candidate => (JsonElement?)candidate.Value).FirstOrDefault()
                    : null;
                if (next is not { } found)
                {
                    // An unknown segment (a list index, say) stays on the schema reached so far.
                    if (node.ValueKind == JsonValueKind.Object && node.TryGetProperty("items", out var items))
                    {
                        node = items;
                    }

                    continue;
                }

                if (IsSecret(found))
                {
                    return true;
                }

                node = found;
            }

            return false;
        }
        catch (JsonException)
        {
            return true;
        }
    }

    static bool IsSecret(JsonElement node)
    {
        if (node.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (node.TryGetProperty(SchemaSynthesizer.ConceptAttributesKeyword, out var attributes) && attributes.ValueKind == JsonValueKind.Object &&
            attributes.EnumerateObject().Any(attribute => string.Equals(attribute.Name, SecretMarker, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // A collection of secrets carries the marker on its item schema.
        return node.TryGetProperty("items", out var items) && IsSecret(items);
    }

    static bool Carries(JsonElement node) =>
        IsSecret(node) ||
        (node.ValueKind == JsonValueKind.Object &&
            ((node.TryGetProperty("properties", out var properties) && properties.ValueKind == JsonValueKind.Object && properties.EnumerateObject().Any(property => Carries(property.Value))) ||
            (node.TryGetProperty("items", out var items) && Carries(items))));

    static Dictionary<string, JsonElement> Members(IEnumerable<(string Name, JsonElement Value)> members, JsonElement schema)
    {
        var declared = schema.TryGetProperty("properties", out var properties) && properties.ValueKind == JsonValueKind.Object
            ? properties.EnumerateObject().ToArray()
            : [];
        var result = new Dictionary<string, JsonElement>();
        foreach (var (name, value) in members)
        {
            var property = declared.Cast<JsonProperty?>().FirstOrDefault(candidate => string.Equals(candidate!.Value.Name, name, StringComparison.OrdinalIgnoreCase));
            if (property is null)
            {
                result[name] = value;
            }
            else if (!IsSecret(property.Value.Value) && Value(value, property.Value.Value) is { } kept)
            {
                result[name] = kept;
            }
        }

        return result;
    }

    // The schema of a self-recursive type is cut to a bare object, so what it could hold is unknown: such a value is
    // dropped (null) rather than echoed, as is any list containing one.
    static JsonElement? Value(JsonElement value, JsonElement schema)
    {
        if (value.ValueKind == JsonValueKind.Array && schema.TryGetProperty("items", out var items))
        {
            var kept = value.EnumerateArray().Select(item => Value(item, items)).ToArray();
            return kept.Any(item => item is null) ? null : JsonSerializer.SerializeToElement(kept);
        }

        // A value of the wrong shape where the schema holds secrets cannot be filtered, so it is dropped.
        if (value.ValueKind != JsonValueKind.Null && schema.ValueKind == JsonValueKind.Object && Carries(schema) &&
            ((schema.TryGetProperty("items", out _) && value.ValueKind != JsonValueKind.Array) ||
            (schema.TryGetProperty("properties", out _) && value.ValueKind != JsonValueKind.Object)))
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.Object || schema.ValueKind != JsonValueKind.Object)
        {
            return value;
        }

        return schema.TryGetProperty("properties", out _)
            ? JsonSerializer.SerializeToElement(Members(value.EnumerateObject().Select(member => (member.Name, member.Value)), schema))
            : null;
    }
}
