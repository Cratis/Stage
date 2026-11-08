// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Stage.Host;

/// <summary>
/// How the arrays of an expected value are matched against the arrays of an actual value.
/// </summary>
internal enum StageJourneyListMatch
{
    /// <summary>
    /// Each expected element matches a distinct actual element; actual order and extra elements are ignored.
    /// </summary>
    Contains,

    /// <summary>
    /// Expected elements match actual elements in the same relative order; extra elements may sit between them.
    /// </summary>
    Ordered,

    /// <summary>
    /// Both arrays have the same length and element i matches element i.
    /// </summary>
    Exact
}

/// <summary>
/// One place where an actual value does not satisfy the expected value.
/// </summary>
/// <param name="Path">The JSON path of the difference, such as <c language="text">$.items[1].name</c>.</param>
/// <param name="Expected">The expected value at the path, or <see langword="null"/> when nothing was expected there.</param>
/// <param name="Actual">The actual value at the path, or <see langword="null"/> when it is absent.</param>
/// <param name="Reason">Why the values differ.</param>
internal sealed record StageJourneyMismatch(string Path, JsonElement? Expected, JsonElement? Actual, string Reason);

/// <summary>
/// Matches an expected JSON value as a subset of an actual JSON value.
/// </summary>
/// <remarks>
/// Every expected object property must exist in the actual object, compared case-insensitively because the HTTP surface
/// camel-cases; extra actual properties are ignored and an expected <see langword="null"/> matches a <see langword="null"/>
/// or absent value. Strings compare ordinally, numbers by decimal value and booleans by value; a string never matches a number
/// or the reverse. Arrays follow <see cref="StageJourneyListMatch"/>: <see cref="StageJourneyListMatch.Contains"/> and
/// <see cref="StageJourneyListMatch.Ordered"/> pair each expected element with the first unused actual element that matches,
/// in expected order, and report an unmatched element at the expected element's index.
/// </remarks>
internal static class StageJourneyMatching
{
    /// <summary>
    /// Compares an expected value with an actual value.
    /// </summary>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The actual value, or <see langword="null"/> when it is absent.</param>
    /// <param name="list">How arrays are matched.</param>
    /// <param name="path">The path of the compared values.</param>
    /// <returns>Every mismatch; empty when the actual value satisfies the expected value.</returns>
    internal static IReadOnlyList<StageJourneyMismatch> Compare(JsonElement expected, JsonElement? actual, StageJourneyListMatch list, string path = "$")
    {
        var mismatches = new List<StageJourneyMismatch>();
        Match(expected, actual, list, path, mismatches);

        return mismatches;
    }

    /// <summary>
    /// Tells whether an actual value satisfies an expected value.
    /// </summary>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The actual value, or <see langword="null"/> when it is absent.</param>
    /// <param name="list">How arrays are matched.</param>
    /// <returns><see langword="true"/> when nothing differs.</returns>
    internal static bool Matches(JsonElement expected, JsonElement? actual, StageJourneyListMatch list) =>
        Compare(expected, actual, list).Count == 0;

    /// <summary>
    /// Reads a property by name, falling back to a case-insensitive match.
    /// </summary>
    /// <param name="actual">The object to read.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The value, or <see langword="null"/> when the object has no such property.</returns>
    internal static JsonElement? Property(JsonElement actual, string name)
    {
        if (actual.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (actual.TryGetProperty(name, out var exact))
        {
            return exact;
        }

        return actual.EnumerateObject()
            .Where(property => property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            .Select(property => (JsonElement?)property.Value)
            .FirstOrDefault();
    }

    static void Match(JsonElement expected, JsonElement? actual, StageJourneyListMatch list, string path, List<StageJourneyMismatch> mismatches)
    {
        if (expected.ValueKind == JsonValueKind.Null)
        {
            if (actual is { ValueKind: not JsonValueKind.Null })
            {
                mismatches.Add(new(path, expected, actual, "expected null"));
            }

            return;
        }

        if (actual is not { ValueKind: not JsonValueKind.Null } present)
        {
            mismatches.Add(new(path, expected, actual, actual is null ? "missing" : "expected a value, found null"));
            return;
        }

        if (KindOf(expected) != KindOf(present))
        {
            mismatches.Add(new(path, expected, present, $"expected {KindOf(expected)}, found {KindOf(present)}"));
            return;
        }

        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                MatchObject(expected, present, list, path, mismatches);
                break;
            case JsonValueKind.Array:
                MatchArray(expected, present, list, path, mismatches);
                break;
            default:
                if (!ScalarEquals(expected, present))
                {
                    mismatches.Add(new(path, expected, present, "value differs"));
                }
                break;
        }
    }

    static void MatchObject(JsonElement expected, JsonElement actual, StageJourneyListMatch list, string path, List<StageJourneyMismatch> mismatches)
    {
        foreach (var property in expected.EnumerateObject())
        {
            Match(property.Value, Property(actual, property.Name), list, $"{path}.{property.Name}", mismatches);
        }
    }

    static void MatchArray(JsonElement expected, JsonElement actual, StageJourneyListMatch list, string path, List<StageJourneyMismatch> mismatches)
    {
        var expectedItems = expected.EnumerateArray().ToArray();
        var actualItems = actual.EnumerateArray().ToArray();
        if (list == StageJourneyListMatch.Exact)
        {
            if (expectedItems.Length != actualItems.Length)
            {
                mismatches.Add(new(path, Count(expectedItems.Length), Count(actualItems.Length), $"expected {expectedItems.Length} elements, found {actualItems.Length}"));
                return;
            }

            for (var index = 0; index < expectedItems.Length; index++)
            {
                Match(expectedItems[index], actualItems[index], list, $"{path}[{index}]", mismatches);
            }

            return;
        }

        var used = new bool[actualItems.Length];
        var start = 0;
        for (var index = 0; index < expectedItems.Length; index++)
        {
            var found = Find(expectedItems[index], actualItems, used, list == StageJourneyListMatch.Ordered ? start : 0, list);
            if (found < 0)
            {
                var reason = list == StageJourneyListMatch.Ordered ? "no actual element after the previous match matches" : "no unused actual element matches";
                mismatches.Add(new($"{path}[{index}]", expectedItems[index], null, reason));
                continue;
            }

            used[found] = true;
            start = found + 1;
        }
    }

    static int Find(JsonElement expected, JsonElement[] actual, bool[] used, int start, StageJourneyListMatch list)
    {
        for (var index = start; index < actual.Length; index++)
        {
            if (!used[index] && Matches(expected, actual[index], list))
            {
                return index;
            }
        }

        return -1;
    }

    static bool ScalarEquals(JsonElement expected, JsonElement actual) => expected.ValueKind switch
    {
        JsonValueKind.String => string.Equals(expected.GetString(), actual.GetString(), StringComparison.Ordinal),
        JsonValueKind.Number => NumberEquals(expected, actual),
        _ => expected.ValueKind == actual.ValueKind
    };

    static bool NumberEquals(JsonElement expected, JsonElement actual) =>
        expected.TryGetDecimal(out var left) && actual.TryGetDecimal(out var right)
            ? left == right
            : expected.GetDouble().Equals(actual.GetDouble());

    static string KindOf(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => "an object",
        JsonValueKind.Array => "an array",
        JsonValueKind.String => "a string",
        JsonValueKind.Number => "a number",
        JsonValueKind.True or JsonValueKind.False => "a boolean",
        _ => "null"
    };

    static JsonElement Count(int count) => JsonSerializer.SerializeToElement(count);
}
