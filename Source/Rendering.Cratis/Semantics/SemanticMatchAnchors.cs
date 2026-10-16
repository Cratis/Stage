// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Translates Screenplay's admitted portable match dialect to non-backtracking .NET patterns.
/// </summary>
internal static class SemanticMatchAnchors
{
    // Screenplay MatchCharacterSet uses fixed UTF-16 intervals, not the host's Unicode categories.
    const string Digits = @"\u0030-\u0039";
    const string NotDigits = @"\u0000-\u002f\u003a-\uffff";
    const string Word = @"\u0030-\u0039\u0041-\u005a\u005f\u0061-\u007a";
    const string NotWord = @"\u0000-\u002f\u003a-\u0040\u005b-\u005e\u0060\u007b-\uffff";
    const string Whitespace = @"\u0009-\u000d\u0020\u00a0\u1680\u2000-\u200a\u2028-\u2029\u202f\u205f\u3000\ufeff";
    const string NotWhitespace = @"\u0000-\u0008\u000e-\u001f\u0021-\u009f\u00a1-\u167f\u1681-\u1fff\u200b-\u2027\u202a-\u202e\u2030-\u205e\u2060-\u2fff\u3001-\ufefe\uff00-\uffff";

    /// <summary>
    /// Reports patterns that cannot execute faithfully in the generated non-backtracking matcher.
    /// </summary>
    /// <param name="pattern">The decoded authored pattern.</param>
    /// <returns>The refused construct, or null when the pattern is renderable.</returns>
    internal static string? Refusal(string pattern)
    {
        var check = PortableMatchPattern.Check(pattern);
        if (check.Status != PortableMatchStatus.Admitted)
        {
            return check.Construct ?? check.Status.ToString();
        }

        try
        {
            _ = new Regex(Portable(pattern), RegexOptions.NonBacktracking | RegexOptions.CultureInvariant, Regex.InfiniteMatchTimeout);
        }
        catch (RegexParseException error)
        {
            return $".NET match syntax ({error.Error})";
        }
        catch (NotSupportedException)
        {
            // Portable expansion is bounded by Screenplay, but .NET has a separate automaton-size fuse.
            return "non-backtracking automaton size";
        }

        return null;
    }

    /// <summary>
    /// Rewrites shorthand character sets, dot and dollar anchors without changing their portable meaning.
    /// </summary>
    /// <param name="pattern">The admitted authored pattern.</param>
    /// <returns>The equivalent .NET pattern.</returns>
    internal static string Portable(string pattern)
    {
        var result = new StringBuilder(pattern.Length);
        var inClass = false;
        for (var index = 0; index < pattern.Length; index++)
        {
            var character = pattern[index];
            if (character == '\\')
            {
                var escape = pattern[++index];
                var ranges = Ranges(escape);
                if (ranges is null) result.Append(character).Append(escape);
                else if (inClass) result.Append(ranges);
                else result.Append('[').Append(ranges).Append(']');
                continue;
            }

            if (character == '[' && !inClass) inClass = true;
            else if (character == ']') inClass = false;

            result.Append(character switch
            {
                '$' when !inClass => @"\z",
                '.' when !inClass => @"[^\n\r\u2028\u2029]",
                _ => character.ToString()
            });
        }

        return result.ToString();
    }

    static string? Ranges(char escape) => escape switch
    {
        'd' => Digits,
        'D' => NotDigits,
        'w' => Word,
        'W' => NotWord,
        's' => Whitespace,
        'S' => NotWhitespace,
        _ => null
    };
}
