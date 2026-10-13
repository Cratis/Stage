// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Encodes the deterministic receipt-time portion of a time-ordered strategy.
/// </summary>
internal static class SemanticGenerationTime
{
    const string Crockford = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    internal static string Prefix(SemanticConcept concept, DateTimeOffset instant)
    {
        var generation = concept.Generation!;
        var milliseconds = instant.ToUnixTimeMilliseconds();
        var time = generation.Kind == "uuidv7" ? UuidTime(milliseconds) : CrockfordTime(milliseconds);
        time = generation.Case switch
        {
            "upper" => time.ToUpperInvariant(),
            "lower" => time.ToLowerInvariant(),
            _ => generation.Kind == "typeid" ? time.ToLowerInvariant() : time
        };
        var prefix = generation.Prefix ?? string.Empty;

        return generation.Kind == "typeid" && prefix.Length > 0 ? $"{prefix}_{time}" : $"{prefix}{time}";
    }

    static string UuidTime(long milliseconds)
    {
        var hex = milliseconds.ToString("x12", CultureInfo.InvariantCulture);

        return $"{hex[..8]}-{hex[8..]}";
    }

    static string CrockfordTime(long milliseconds)
    {
        Span<char> result = stackalloc char[10];
        for (var index = result.Length - 1; index >= 0; index--)
        {
            result[index] = Crockford[(int)(milliseconds & 31)];
            milliseconds >>= 5;
        }

        return new string(result);
    }
}
