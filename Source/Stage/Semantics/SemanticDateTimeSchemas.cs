// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Keeps event DateTime text verbatim without changing the shape of an already registered event type.
/// </summary>
public static class SemanticDateTimeSchemas
{
    /// <summary>
    /// Uses plain strings for a new event type and mirrors each DateTime's stored format for an existing one.
    /// </summary>
    /// <param name="built">The schema built from the model.</param>
    /// <param name="stored">The existing schema, or null for a new event type.</param>
    /// <returns>A schema that preserves stored formats.</returns>
    public static JsonObject ForStorage(JsonObject built, JsonObject? stored)
    {
        var result = built.DeepClone().AsObject();
        foreach (var (property, counterpart, _) in DateTimes(result, stored, string.Empty))
        {
            if (stored is not null && counterpart is null) continue;
            property.Remove("format");
            if (counterpart?["format"] is { } format) property["format"] = format.DeepClone();
        }

        return result;
    }

    /// <summary>
    /// Names DateTime properties whose stored schema does not keep their text verbatim.
    /// </summary>
    /// <param name="built">The schema built from the model.</param>
    /// <param name="stored">The schema read from Chronicle, or null when unknown.</param>
    /// <returns>The paths that cannot be reconstructed exactly.</returns>
    public static IEnumerable<string> LossyProperties(JsonObject built, JsonObject? stored) =>
        DateTimes(built, stored, string.Empty).Where(property => !IsVerbatim(property.Stored)).Select(property => property.Path);

    static bool IsVerbatim(JsonObject? schema) =>
        schema is not null && HasType(schema["type"], "string") && schema["format"] is null;

    static bool HasType(JsonNode? type, string expected) => type switch
    {
        JsonValue value when value.TryGetValue<string>(out var name) => name == expected,
        JsonArray types => types.Any(item => HasType(item, expected)),
        _ => false
    };

    static IEnumerable<(JsonObject Built, JsonObject? Stored, string Path)> DateTimes(JsonObject built, JsonObject? stored, string path)
    {
        stored = Scalar(stored);
        if (built["format"]?.GetValue<string>() == "date-time") yield return (built, stored, path);
        foreach (var child in Children(built, stored, path))
        {
            foreach (var property in DateTimes(child.Built, child.Stored, child.Path)) yield return property;
        }
    }

    static IEnumerable<(JsonObject Built, JsonObject? Stored, string Path)> Children(JsonObject built, JsonObject? stored, string path)
    {
        if (built["properties"] is JsonObject properties)
        {
            foreach (var property in properties)
            {
                yield return (property.Value!.AsObject(), (stored?["properties"] as JsonObject)?[property.Key] as JsonObject, Join(path, property.Key));
            }
        }

        if (built["items"] is JsonObject items) yield return (items, stored?["items"] as JsonObject, path);
        foreach (var branch in Branches(built)) yield return (branch, stored, path);
    }

    static IEnumerable<JsonObject> Branches(JsonObject schema) =>
        ((schema["anyOf"] ?? schema["oneOf"]) as JsonArray ?? []).OfType<JsonObject>().Where(branch => branch["type"] is not JsonValue type || !HasType(type, "null"));

    static JsonObject? Scalar(JsonObject? schema) => schema is null ? null : Branches(schema).FirstOrDefault() ?? schema;

    static string Join(string parent, string property) => string.IsNullOrEmpty(parent) ? property : $"{parent}.{property}";
}
