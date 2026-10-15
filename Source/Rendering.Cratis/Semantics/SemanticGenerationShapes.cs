// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Defines Stage's independent strategy shapes, including canonical padding bits.
/// </summary>
internal static class SemanticGenerationShapes
{
    internal const string FreshnessMessage = "A generated event source already has facts; retry the command with a fresh generated value.";
    const string Crockford = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    const string Base64Url = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
    const string Base32 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    const string NanoIdAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-";

    // Match Screenplay's UsesConceptGeneration: later schema versions alone do not enable freshness or strategy checks.
    internal static bool UsesConceptGeneration(SemanticApplication application) => application.Concepts.Any(concept => concept.Generation is not null);

    internal static bool Supported(SemanticConcept concept) => concept.Primitive switch
    {
        SemanticPrimitiveType.Uuid => concept.Generation is null || IsUuid(concept.Generation.Kind),
        SemanticPrimitiveType.Text => concept.Generation is { } generation && KnownKind(generation.Kind),
        _ => false
    };

    internal static bool Matches(SemanticConcept concept, SemanticValue value) => value is SemanticTextValue text &&
        Regex.IsMatch(text.Value, Pattern(concept), RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    internal static string FixtureMessage(string property) => $"Generated fixture for '{property}' does not match its generation strategy shape.";

    internal static string RuleMessage(string property, string concept) => $"Generated fixture '{property}' fails concept '{concept}' rules";

    internal static string Pattern(SemanticConcept concept)
    {
        var generation = concept.Generation ?? new SemanticGeneration("uuid");

        return $"\\A{Regex.Escape(Prefix(generation))}{Shape(generation)}\\z";
    }

    static string Prefix(SemanticGeneration generation) => generation.Prefix is { Length: > 0 } prefix &&
        string.Equals(generation.Kind, "typeid", StringComparison.Ordinal) ? prefix + "_" : generation.Prefix ?? string.Empty;

    static string Shape(SemanticGeneration generation) => generation.Kind switch
    {
        "uuid" or "uuidv7" => Uuid(generation),
        "ulid" => $"[0-7]{Symbols(Case(Crockford, generation.Case, true), 25)}",
        "typeid" => TypeId(),
        "nanoid" => Symbols(generation.Alphabet ?? NanoIdAlphabet, generation.Size ?? 21),
        "bytes" => Bytes(generation),
        _ => "(?!)"
    };

    static bool IsUuid(string kind) => string.Equals(kind, "uuid", StringComparison.Ordinal) || string.Equals(kind, "uuidv7", StringComparison.Ordinal);

    static bool KnownKind(string kind) => kind switch
    {
        "uuid" or "uuidv7" or "ulid" or "typeid" or "nanoid" or "bytes" => true,
        _ => false
    };

    static string Uuid(SemanticGeneration generation)
    {
        var upper = string.Equals(generation.Case, "upper", StringComparison.Ordinal);
        var digits = upper ? "0123456789ABCDEF" : "0123456789abcdef";
        var variant = upper ? "89AB" : "89ab";
        var version = string.Equals(generation.Kind, "uuidv7", StringComparison.Ordinal) ? '7' : '4';

        return $"{Symbols(digits, 8)}-{Symbols(digits, 4)}-{version}{Symbols(digits, 3)}-{Symbols(variant, 1)}{Symbols(digits, 3)}-{Symbols(digits, 12)}";
    }

    static string TypeId()
    {
        var alphabet = Crockford.ToLowerInvariant();

        // 26 Crockford symbols encode 128 bits: the leading symbol holds three bits.
        // UUID v7's version occupies symbol 10; the RFC variant occupies symbol 13.
        return $"[0-7]{Symbols(alphabet, 9)}[ef]{Symbols(alphabet, 2)}[89abrstv]{Symbols(alphabet, 12)}";
    }

    static string Bytes(SemanticGeneration generation)
    {
        var count = generation.Count!.Value;

        return generation.Encoding switch
        {
            "hex" => Symbols(Case("0123456789abcdef", generation.Case, false), count * 2),
            "base32" => EncodedBytes(count, Case(Base32, generation.Case, true), 5),
            "base64url" => EncodedBytes(count, Base64Url, 6),
            _ => "(?!)"
        };
    }

    static string EncodedBytes(int count, string alphabet, int bitsPerSymbol)
    {
        var length = ((count * 8) + bitsPerSymbol - 1) / bitsPerSymbol;
        var unused = (length * bitsPerSymbol) - (count * 8);
        var last = new string([.. alphabet.Where((_, index) => index % (1 << unused) == 0)]);

        return $"{Symbols(alphabet, length - 1)}{Symbols(last, 1)}";
    }

    static string Symbols(string alphabet, int count) => count == 0 ? string.Empty :
        $"[{string.Concat(alphabet.Select(character => $"\\u{((int)character).ToString("x4", CultureInfo.InvariantCulture)}"))}]{{{count.ToString(CultureInfo.InvariantCulture)}}}";

    static string Case(string value, string? casing, bool upperDefault) => string.Equals(casing, "upper", StringComparison.Ordinal) || (casing is null && upperDefault)
        ? value.ToUpperInvariant() : value.ToLowerInvariant();
}
