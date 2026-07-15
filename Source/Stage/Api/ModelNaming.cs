// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Api;

/// <summary>
/// Naming helpers for turning modeled names into valid CLR identifiers and conventional query names.
/// </summary>
public static class ModelNaming
{
    /// <summary>
    /// Sanitizes a modeled name into a valid CLR identifier (letters and digits only).
    /// </summary>
    /// <param name="value">The modeled name.</param>
    /// <returns>A non-empty identifier safe to use as a type or namespace segment.</returns>
    public static string ToIdentifier(string value)
    {
        var identifier = new string([.. value.Where(char.IsLetterOrDigit)]);
        if (identifier.Length == 0)
        {
            return "Item";
        }

        return char.IsDigit(identifier[0]) ? $"_{identifier}" : identifier;
    }

    /// <summary>
    /// Produces a simple English plural of the given identifier, used for the <c>All&lt;Plural&gt;</c> query name.
    /// </summary>
    /// <param name="value">The singular identifier.</param>
    /// <returns>The pluralized identifier.</returns>
    public static string Pluralize(string value)
    {
        if (value.Length == 0)
        {
            return value;
        }

        if (value.EndsWith('y') && value.Length > 1 && !IsVowel(value[^2]))
        {
            return $"{value[..^1]}ies";
        }

        if (EndsWithAny(value, "s", "x", "z", "ch", "sh"))
        {
            return $"{value}es";
        }

        return $"{value}s";
    }

    static bool IsVowel(char character) => "aeiouAEIOU".Contains(character);

    static bool EndsWithAny(string value, params string[] suffixes) =>
        suffixes.Any(suffix => value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
}
