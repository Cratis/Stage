// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Host;

internal static partial class SemanticWorldRebuilder
{
    internal static ImmutableArray<SemanticPropertyValue> Values(JsonElement json, ImmutableArray<SemanticProperty> properties, SemanticApplication application)
    {
        if (!MatchesSchema(json, properties))
        {
            throw new SemanticWorldRebuildRefused("Chronicle content does not match its semantic property schema.");
        }

        return [.. properties.Select(property => new SemanticPropertyValue(property.Id, PropertyValue(json, property, application)))];
    }

    static ImmutableArray<SemanticPropertyValue> CompositeValues(JsonElement json, ImmutableArray<SemanticProperty> properties, SemanticApplication application)
    {
        if (!MatchesSchema(json, properties))
        {
            throw new SemanticWorldRebuildRefused("Chronicle content does not match its semantic property schema.");
        }

        // Composite absence and supplied order are semantic data, not missing top-level event columns.
        return [.. json.EnumerateObject().Select(member =>
        {
            var property = properties.Single(property => property.Name == member.Name);
            return new SemanticPropertyValue(property.Id, Value(member.Value, property.Type, application));
        })];
    }

    // Every stored name is declared exactly once and appears once. Chronicle may omit an absent optional value
    // (it does for protected concepts), so only optional properties may be missing.
    static bool MatchesSchema(JsonElement json, ImmutableArray<SemanticProperty> properties)
    {
        if (json.ValueKind != JsonValueKind.Object) return false;
        var names = json.EnumerateObject().Select(entry => entry.Name).ToArray();
        if (names.Distinct(StringComparer.Ordinal).Count() != names.Length) return false;
        if (names.Any(name => properties.Count(property => property.Name == name) != 1)) return false;

        return properties.All(property => property.Type.IsOptional || names.Contains(property.Name, StringComparer.Ordinal));
    }

    static SemanticValue PropertyValue(JsonElement json, SemanticProperty property, SemanticApplication application) =>
        json.TryGetProperty(property.Name, out var value) ? Value(value, property.Type, application) : SemanticValue.Null;

    static int EnumerationIndex(SemanticConcept concept, int number)
    {
        if (concept.Numbers is { } numbers) return numbers.IndexOf(number);

        return number >= 0 && number < concept.Values.Length ? number : -1;
    }

    static SemanticValue Text(string value, SemanticTypeReference type, SemanticApplication application) => Value(JsonSerializer.SerializeToElement(value), type, application);

    static SemanticValue Value(JsonElement json, SemanticTypeReference type, SemanticApplication application)
    {
        if (json.ValueKind == JsonValueKind.Null && type.IsOptional)
        {
            return SemanticValue.Null;
        }

        if (type.IsCollection && json.ValueKind == JsonValueKind.Array)
        {
            return SemanticValue.Array([.. json.EnumerateArray().Select(item => Value(item, type with { IsCollection = false, IsOptional = false }, application))]);
        }

        if (type.Kind == SemanticTypeReferenceKind.CompositeType && json.ValueKind == JsonValueKind.Object)
        {
            var composite = application.Types.Single(candidate => candidate.Id == type.Target);
            return SemanticValue.Composite(CompositeValues(json, composite.Properties, application));
        }

        var concept = type.Kind == SemanticTypeReferenceKind.Concept
            ? application.Concepts.Single(candidate => candidate.Id == type.Target)
            : null;
        if (concept?.Values is { IsEmpty: false })
        {
            return EnumeratedValue(json, concept);
        }

        return PrimitiveValue(concept?.Primitive ?? type.Primitive, json);
    }

    static SemanticValue EnumeratedValue(JsonElement json, SemanticConcept concept)
    {
        var allowed = concept.Values;
        return json.ValueKind switch
        {
            JsonValueKind.Number when json.TryGetInt32(out var number) && EnumerationIndex(concept, number) is var index && index >= 0 => SemanticValue.Text(allowed[index]),
            JsonValueKind.String when allowed.Contains(json.GetString()!, StringComparer.Ordinal) => SemanticValue.Text(json.GetString()!),
            JsonValueKind.String => throw new SemanticWorldRebuildRefused("Chronicle content contains a value outside its concept enumeration."),
            _ => throw IncompatibleValue()
        };
    }

    static SemanticValue PrimitiveValue(SemanticPrimitiveType primitive, JsonElement json) => json.ValueKind switch
    {
        JsonValueKind.String when IsValidText(primitive, json.GetString()) => SemanticValue.Text(json.GetString()!),
        JsonValueKind.True when primitive == SemanticPrimitiveType.Boolean => SemanticValue.Boolean(true),
        JsonValueKind.False when primitive == SemanticPrimitiveType.Boolean => SemanticValue.Boolean(false),
        JsonValueKind.Number when TryNumber(primitive, json, out var number) => SemanticValue.Number(number),
        _ => throw IncompatibleValue()
    };

    static SemanticWorldRebuildRefused IncompatibleValue() => new("Chronicle content contains a value incompatible with its semantic type.");

    static bool IsValidText(SemanticPrimitiveType primitive, string? text) => primitive switch
    {
        SemanticPrimitiveType.Text => true,
        SemanticPrimitiveType.Uuid => Guid.TryParseExact(text, "D", out var guid) && guid.ToString("D") == text,
        SemanticPrimitiveType.Date => DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
        SemanticPrimitiveType.DateTime => DateTimeOffset.TryParseExact(text, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _),
        _ => false
    };

    static bool TryNumber(SemanticPrimitiveType primitive, JsonElement json, out decimal number)
    {
        number = 0;
        return primitive switch
        {
            SemanticPrimitiveType.DecimalNumber => json.TryGetDecimal(out number),
            SemanticPrimitiveType.WholeNumber => json.TryGetDecimal(out number) && decimal.Truncate(number) == number,
            _ => false
        };
    }
}
