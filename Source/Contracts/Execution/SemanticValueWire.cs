// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Execution;

/// <summary>Preserves the portable value kind across the worker pipe.</summary>
public static class SemanticValueWire
{
    /// <summary>Encodes a portable value. Dates, identities and enum names remain portable text.</summary>
    /// <param name="value">The portable value to encode.</param>
    /// <exception cref="ArgumentException">The value kind is not a portable value.</exception>
    public static JsonElement Write(SemanticValue value) => value switch
    {
        SemanticNullValue => JsonSerializer.SerializeToElement(new { kind = "null" }),
        SemanticBooleanValue boolean => JsonSerializer.SerializeToElement(new { kind = "boolean", value = boolean.Value }),
        SemanticNumberValue number => JsonSerializer.SerializeToElement(new { kind = "number", value = number.Value.ToString(CultureInfo.InvariantCulture) }),
        SemanticTextValue text => JsonSerializer.SerializeToElement(new { kind = "text", value = text.Value }),
        SemanticArrayValue array => JsonSerializer.SerializeToElement(new { kind = "array", value = array.Values.Select(Write).ToArray() }),
        SemanticCompositeValue composite => JsonSerializer.SerializeToElement(new
        {
            kind = "composite",
            value = composite.Properties.Select(property => new { property = property.TargetProperty.ToString(), value = Write(property.Value) }).ToArray()
        }),
        _ => throw new ArgumentException("Unknown portable value kind.", nameof(value))
    };

    /// <summary>Decodes a tagged portable value.</summary>
    /// <param name="wire">The tagged JSON value.</param>
    /// <exception cref="JsonException">The wire shape is not exactly what <see cref="Write"/> emits.</exception>
    public static SemanticValue Read(JsonElement wire)
    {
        RequireObject(wire);
        var kind = wire.TryGetProperty("kind", out var kindElement) ? RequiredString(kindElement) : throw new JsonException("Invalid portable value structure.");
        RequireShape(wire, kind == "null" ? ["kind"] : ["kind", "value"]);
        return kind switch
        {
            "null" => SemanticValue.Null,
            "boolean" => SemanticValue.Boolean(RequiredBoolean(wire.GetProperty("value"))),
            "number" => SemanticValue.Number(RequiredNumber(wire.GetProperty("value"))),
            "text" => SemanticValue.Text(RequiredString(wire.GetProperty("value"))),
            "array" => SemanticValue.Array([.. RequiredArray(wire.GetProperty("value")).Select(Read)]),
            "composite" => SemanticValue.Composite([.. RequiredArray(wire.GetProperty("value")).Select(ReadProperty)]),
            _ => throw new JsonException("Unknown portable value kind.")
        };
    }

    static SemanticPropertyValue ReadProperty(JsonElement property)
    {
        RequireObject(property);
        RequireShape(property, ["property", "value"]);
        return new(ParseId(RequiredString(property.GetProperty("property"))), Read(property.GetProperty("value")));
    }

    static SemanticId ParseId(string id)
    {
        try
        {
            return SemanticId.Parse(id);
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException or InvalidOperationException)
        {
            throw new JsonException("A portable property id is invalid.", exception);
        }
    }

    static void RequireObject(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object) throw new JsonException("A portable value must be a JSON object.");
    }

    static JsonElement.ArrayEnumerator RequiredArray(JsonElement value) => value.ValueKind == JsonValueKind.Array
        ? value.EnumerateArray()
        : throw new JsonException("A portable collection must be a JSON array.");

    static bool RequiredBoolean(JsonElement value) => value.ValueKind is JsonValueKind.True or JsonValueKind.False
        ? value.GetBoolean()
        : throw new JsonException("A portable boolean must be a JSON boolean.");

    static decimal RequiredNumber(JsonElement value) =>
        decimal.TryParse(RequiredString(value), NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
            ? number
            : throw new JsonException("A portable number must be a decimal string.");

    // Every string slot is required to be a JSON string, not JSON null; GetString alone permits null.
    static string RequiredString(JsonElement value) => value.ValueKind == JsonValueKind.String
        ? value.GetString()!
        : throw new JsonException("A portable string must be a non-null JSON string.");

    // Match the writer's closed grammar at every level, rejecting missing, duplicate and unknown fields.
    static void RequireShape(JsonElement value, string[] names)
    {
        var fields = value.EnumerateObject().Select(field => field.Name).ToArray();
        if (fields.Length != names.Length || !names.All(name => fields.Count(field => field == name) == 1))
            throw new JsonException("Invalid portable value structure.");
    }
}
