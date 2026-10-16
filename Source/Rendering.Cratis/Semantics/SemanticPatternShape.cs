// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Translates already validated Screenplay finite patterns into membership checks, never a sampler.
/// </summary>
internal static class SemanticPatternShape
{
    internal static bool HasDates(SemanticGeneration? generation) => generation?.Kind == "pattern" &&
        Tokens(generation.Pattern!, null).Any(token => token.Date);

    internal static string Pattern(SemanticGeneration generation, DateTimeOffset? instant) =>
        string.Concat(Tokens(generation.Pattern!, instant).Select(token => token.Expression)) + generation.Checksum switch
        {
            "luhn" or "mod11" => "[0-9]",
            "mod97" => "[0-9]{2}",
            _ => string.Empty
        };

    // Matches GenerationPatternToken.Expand: without an authored clock, date positions are independent digits.
    // The reference checks only suffix shape here; checksum arithmetic is an authored validation rule.
    static IEnumerable<(string Expression, bool Date)> Tokens(string pattern, DateTimeOffset? instant)
    {
        var offset = 0;
        while (offset < pattern.Length)
        {
            var character = pattern[offset++];
            var expression = character switch
            {
                '#' => "[0-9]",
                '?' => "[A-Z]",
                '*' => "[0-9A-Z]",
                '\\' => Regex.Escape(pattern[offset++].ToString()),
                '[' => CharacterClass(pattern, ref offset),
                '{' => DateSegment(pattern, ref offset, instant),
                _ => Regex.Escape(character.ToString())
            };
            var date = character == '{';
            if (offset + 1 < pattern.Length && pattern[offset] == '{' && char.IsAsciiDigit(pattern[offset + 1]))
            {
                var end = pattern.IndexOf('}', offset);
                expression = $"(?:{expression}){pattern[offset..(end + 1)]}";
                offset = end + 1;
            }
            yield return (expression, date);
        }
    }

    static string DateSegment(string pattern, ref int offset, DateTimeOffset? instant)
    {
        var end = pattern.IndexOf('}', offset);
        var segment = pattern[offset..end];
        offset = end + 1;

        return instant is { } time
            ? Regex.Escape(DateOnly.FromDateTime(time.UtcDateTime).ToString(segment, CultureInfo.InvariantCulture))
            : $"[0-9]{{{(segment == "yyyy" ? 4 : 2)}}}";
    }

    static string CharacterClass(string pattern, ref int offset)
    {
        var result = new StringBuilder("[");
        while (pattern[offset] != ']')
        {
            var first = ClassCharacter(pattern, ref offset);
            var last = first;
            if (pattern[offset] == '-')
            {
                offset++;
                last = ClassCharacter(pattern, ref offset);
            }
            for (var character = (int)first; character <= last; character++)
            {
                result.Append("\\u").Append(character.ToString("x4", CultureInfo.InvariantCulture));
            }
        }
        offset++;

        return result.Append(']').ToString();
    }

    static char ClassCharacter(string pattern, ref int offset)
    {
        var character = pattern[offset++];

        return character == '\\' ? pattern[offset++] : character;
    }
}
