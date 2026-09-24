// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Semantics;

/// <summary>
/// The exception that is thrown when an incoming JSON value cannot be bound to its semantic contract.
/// </summary>
/// <param name="details">The reason the value cannot be bound.</param>
public sealed class UnsupportedSemanticValue(string details = "The command payload contains an unsupported semantic value.") : Exception(details);

internal static class SemanticJsonValues
{
    internal static ImmutableArray<SemanticPropertyValue> Bind(
        IEnumerable<SemanticProperty> properties,
        IReadOnlyDictionary<string, JsonElement> payload,
        ExecutableSemanticModel model) =>
        [.. properties.Select(property => new SemanticPropertyValue(
            property.Id,
            payload.TryGetValue(property.Name, out var value) ? Bind(value, property.Type, model) : SemanticValue.Null))];

    internal static SemanticValue Bind(JsonElement value, SemanticTypeReference type, ExecutableSemanticModel model)
    {
        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return SemanticValue.Null;
        }

        if (type.IsCollection)
        {
            return value.ValueKind == JsonValueKind.Array
                ? SemanticValue.Array([.. value.EnumerateArray().Select(item => Bind(item, type with { IsCollection = false }, model))])
                : BindUntyped(value);
        }

        if (type.Kind == SemanticTypeReferenceKind.CompositeType && value.ValueKind == JsonValueKind.Object)
        {
            var properties = model.Application.Types.Single(candidate => candidate.Id == type.Target).Properties;
            var members = value.EnumerateObject().ToDictionary(item => item.Name, item => item.Value);
            var unknown = members.Keys.FirstOrDefault(name => properties.All(property => property.Name != name));
            if (unknown is not null)
            {
                throw new UnsupportedSemanticValue($"Unknown composite property '{unknown}'.");
            }

            return SemanticValue.Composite(Bind(properties, members, model));
        }

        return BindUntyped(value);
    }

    internal static object? ToObject(SemanticValue value) => ToObject(value, null, null);

    internal static object? ToObject(SemanticValue value, SemanticTypeReference? type, SemanticApplication? application) => value switch
    {
        SemanticNullValue => null,
        SemanticTextValue text => text.Value,
        SemanticNumberValue number => number.Value,
        SemanticBooleanValue boolean => boolean.Value,
        SemanticArrayValue array => array.Values.Select(item => ToObject(item, type is null ? null : type with { IsCollection = false }, application)).ToArray(),
        SemanticCompositeValue composite when type?.Kind == SemanticTypeReferenceKind.CompositeType && application is not null =>
            composite.Properties.ToDictionary(
                entry => application.Types.Single(candidate => candidate.Id == type.Target).Properties.Single(property => property.Id == entry.TargetProperty).Name,
                entry =>
                {
                    var property = application.Types.Single(candidate => candidate.Id == type.Target).Properties.Single(candidate => candidate.Id == entry.TargetProperty);
                    return ToObject(entry.Value, property.Type, application);
                }),
        _ => throw new UnsupportedSemanticValue()
    };

    static SemanticValue BindUntyped(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => SemanticValue.Text(value.GetString()!),
        JsonValueKind.Number => value.TryGetDecimal(out var number)
            ? SemanticValue.Number(number)
            : throw new UnsupportedSemanticValue("The numeric value is outside the supported decimal range."),
        JsonValueKind.True => SemanticValue.Boolean(true),
        JsonValueKind.False => SemanticValue.Boolean(false),
        JsonValueKind.Null => SemanticValue.Null,
        JsonValueKind.Array => SemanticValue.Array([.. value.EnumerateArray().Select(BindUntyped)]),
        _ => throw new UnsupportedSemanticValue()
    };
}
