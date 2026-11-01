// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderModelSchema.given;

internal sealed class strict_schema_validator
{
    static readonly HashSet<string> Keywords = ["type", "properties", "required", "items", "enum", "anyOf", "oneOf", "$ref", "additionalProperties", "const"];
    static readonly Dictionary<string, JsonValueKind[]> Kinds = new(StringComparer.Ordinal)
    {
        ["object"] = [JsonValueKind.Object],
        ["array"] = [JsonValueKind.Array],
        ["string"] = [JsonValueKind.String],
        ["boolean"] = [JsonValueKind.True, JsonValueKind.False],
        ["null"] = [JsonValueKind.Null],
        ["number"] = [JsonValueKind.Number]
    };
    readonly JsonNode _root;

    internal strict_schema_validator(JsonNode schema)
    {
        _root = schema;
        CheckKeywords(schema, isRoot: true);
    }

    internal bool Validate(JsonElement document) => Matches(_root, document);

    static void CheckKeywords(JsonNode schema, bool isRoot = false)
    {
        if (schema is not JsonObject obj) return;
        foreach (var (keyword, value) in obj)
        {
            // $id is root identification metadata, not a validation keyword.
            if (isRoot && keyword == "$id") continue;
            if (!Keywords.Contains(keyword)) throw new unsupported_schema_keyword(keyword);
            CheckChildren(keyword, value);
        }
    }

    static void CheckChildren(string keyword, JsonNode? value)
    {
        foreach (var child in Children(keyword, value)) CheckKeywords(child!);
    }

    static IEnumerable<JsonNode?> Children(string keyword, JsonNode? value) => keyword switch
    {
        "properties" => value!.AsObject().Select(property => property.Value),
        "anyOf" or "oneOf" => value!.AsArray(),
        "items" or "additionalProperties" => [value],
        _ => []
    };

    bool Matches(JsonNode schema, JsonElement document)
    {
        if (schema is JsonValue boolean) return boolean.GetValue<bool>();
        var obj = schema.AsObject();

        return MatchesType(obj["type"], document)
            && MatchesValue(obj, document)
            && MatchesReference(obj["$ref"], document)
            && MatchesBranches(obj["anyOf"], document, exactlyOne: false)
            && MatchesBranches(obj["oneOf"], document, exactlyOne: true)
            && MatchesObject(obj, document)
            && MatchesItems(obj["items"], document);
    }

    static bool MatchesType(JsonNode? type, JsonElement document) => type is null || (type is JsonArray alternatives ? alternatives.Any(item => IsType(item!.GetValue<string>(), document)) : IsType(type.GetValue<string>(), document));

    static bool IsType(string type, JsonElement document)
    {
        if (type == "integer") return IsInteger(document);
        if (!Kinds.TryGetValue(type, out var kinds)) throw new unsupported_schema_keyword($"type:{type}");

        return kinds.Contains(document.ValueKind);
    }

    static bool IsInteger(JsonElement document) => document.ValueKind == JsonValueKind.Number && document.TryGetDecimal(out var number) && decimal.IsInteger(number);

    static bool MatchesValue(JsonObject schema, JsonElement document)
    {
        var value = JsonNode.Parse(document.GetRawText());

        return (!schema.ContainsKey("const") || JsonNode.DeepEquals(schema["const"], value))
            && (schema["enum"] is not JsonArray values || values.Any(candidate => JsonNode.DeepEquals(candidate, value)));
    }

    bool MatchesReference(JsonNode? reference, JsonElement document)
    {
        if (reference is null) return true;
        var pointer = reference.GetValue<string>();
        if (!pointer.StartsWith("#/", StringComparison.Ordinal)) throw new unsupported_schema_keyword($"$ref:{pointer}");
        var target = pointer[2..].Split('/').Aggregate(_root, (node, part) => (node is JsonArray array ? array[int.Parse(part, System.Globalization.CultureInfo.InvariantCulture)] : node[part.Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal)]) ?? throw new unsupported_schema_keyword($"unresolved $ref:{pointer}"));

        return Matches(target, document);
    }

    bool MatchesBranches(JsonNode? branches, JsonElement document, bool exactlyOne)
    {
        if (branches is null) return true;
        var count = branches.AsArray().Count(branch => Matches(branch!, document));

        return exactlyOne ? count == 1 : count > 0;
    }

    bool MatchesObject(JsonObject schema, JsonElement document)
    {
        if (document.ValueKind != JsonValueKind.Object) return true;
        if (schema["required"] is JsonArray required && required.Any(name => !document.TryGetProperty(name!.GetValue<string>(), out _))) return false;
        var properties = schema["properties"] as JsonObject;

        return document.EnumerateObject().All(property => properties?.TryGetPropertyValue(property.Name, out var contract) == true
            ? Matches(contract!, property.Value)
            : schema["additionalProperties"] is not { } additional || Matches(additional, property.Value));
    }

    bool MatchesItems(JsonNode? items, JsonElement document) => items is null || document.ValueKind != JsonValueKind.Array || document.EnumerateArray().All(item => Matches(items, item));
}

internal sealed class unsupported_schema_keyword(string keyword) : Exception($"Unsupported schema keyword '{keyword}'.");
