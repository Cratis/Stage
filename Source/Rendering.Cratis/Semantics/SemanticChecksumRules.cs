// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static class SemanticChecksumRules
{
    internal static string? Algorithm(SemanticValue? operand) => operand is SemanticTextValue text ? text.Value switch
    {
        "luhn" => "Luhn",
        "mod11" => "Mod11",
        "mod97" => "Mod97",
        _ => null
    } : null;

    internal static bool Satisfies(SemanticValue value, SemanticValue? operand, Func<Exception> unsupported) =>
        value is SemanticTextValue text && Algorithm(operand) is { } algorithm
            ? Matches(text.Value, algorithm)
            : throw unsupported();

    // Mirrors Cratis/Screenplay v4.180.1, Source/DotNET/Screenplay/Semantics/Generation/GenerationChecksum.cs.
    // Keep this shared rule evaluator target-neutral; Fundamentals is referenced only by emitted application code.
    static bool Matches(string value, string algorithm)
    {
        var width = algorithm == "Mod97" ? 2 : 1;
        if (value.Length < width || !value.All(character => character is >= ' ' and <= '~')) return false;
        var suffix = value[^width..];
        if (!suffix.All(character => character is >= '0' and <= '9')) return false;
        var digits = value[..^width].Where(character => character is >= '0' and <= '9').Select(character => character - '0').ToArray();
        var check = algorithm switch
        {
            "Luhn" => Luhn(digits),
            "Mod11" => Mod11(digits),
            _ => Mod97(digits)
        };

        return !(algorithm == "Mod11" && check == 10) &&
            string.Equals(suffix, check.ToString(algorithm == "Mod97" ? "D2" : "D1", CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    static int Luhn(int[] digits)
    {
        var sum = 0;
        for (var index = 0; index < digits.Length; index++)
        {
            var digit = digits[digits.Length - index - 1] * (index % 2 == 0 ? 2 : 1);
            sum = (sum + (digit > 9 ? digit - 9 : digit)) % 10;
        }

        return (10 - (sum % 10)) % 10;
    }

    static int Mod11(int[] digits)
    {
        var sum = 0;
        for (var index = 0; index < digits.Length; index++) sum = (sum + (digits[digits.Length - index - 1] * (2 + (index % 6)))) % 11;

        return (11 - (sum % 11)) % 11;
    }

    static int Mod97(int[] digits)
    {
        var residue = 0;
        foreach (var digit in digits) residue = ((residue * 10) + digit) % 97;

        var shifted = residue * 100;
        var remainder = shifted % 97;

        return 98 - remainder;
    }
}
