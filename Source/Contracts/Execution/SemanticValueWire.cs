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
    /// <exception cref="ArgumentException">The value is of an unknown portable kind.</exception>
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
    /// <param name="wire">The tagged wire element.</param>
    /// <exception cref="JsonException">The tag names an unknown portable kind.</exception>
    public static SemanticValue Read(JsonElement wire) => wire.GetProperty("kind").GetString() switch
    {
        "null" => SemanticValue.Null,
        "boolean" => SemanticValue.Boolean(wire.GetProperty("value").GetBoolean()),
        "number" => SemanticValue.Number(decimal.Parse(wire.GetProperty("value").GetString()!, CultureInfo.InvariantCulture)),
        "text" => SemanticValue.Text(wire.GetProperty("value").GetString()!),
        "array" => SemanticValue.Array([.. wire.GetProperty("value").EnumerateArray().Select(Read)]),
        "composite" => SemanticValue.Composite([.. wire.GetProperty("value").EnumerateArray().Select(property =>
            new SemanticPropertyValue(SemanticId.Parse(property.GetProperty("property").GetString()!), Read(property.GetProperty("value"))))]),
        _ => throw new JsonException("Unknown portable value kind.")
    };
}
