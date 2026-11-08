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
    // A concept declared with the sensitive marker is the secret of the schema form; a personal-data marker on the same
    // concept takes precedence, exactly as it does when the concept is rendered.
    const string SecretMarker = "sensitive";
    const string PersonalMarker = "pii";

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
            return payload;
        }
    }

    static bool IsSecret(JsonElement node)
    {
        if (node.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (node.TryGetProperty(SchemaSynthesizer.ConceptAttributesKeyword, out var attributes) && attributes.ValueKind == JsonValueKind.Object &&
            attributes.EnumerateObject().Any(attribute => string.Equals(attribute.Name, SecretMarker, StringComparison.OrdinalIgnoreCase)) &&
            !attributes.EnumerateObject().Any(attribute => string.Equals(attribute.Name, PersonalMarker, StringComparison.OrdinalIgnoreCase)))
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
            else if (!IsSecret(property.Value.Value))
            {
                result[name] = Value(value, property.Value.Value);
            }
        }

        return result;
    }

    static JsonElement Value(JsonElement value, JsonElement schema)
    {
        if (value.ValueKind == JsonValueKind.Array && schema.TryGetProperty("items", out var items))
        {
            return JsonSerializer.SerializeToElement(value.EnumerateArray().Select(item => Value(item, items)).ToArray());
        }

        return value.ValueKind == JsonValueKind.Object && schema.ValueKind == JsonValueKind.Object && schema.TryGetProperty("properties", out _)
            ? JsonSerializer.SerializeToElement(Members(value.EnumerateObject().Select(member => (member.Name, member.Value)), schema))
            : value;
    }
}
