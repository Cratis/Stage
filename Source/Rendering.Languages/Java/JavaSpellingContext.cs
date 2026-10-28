// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Checks the text around each pack spelling so that the spelling cannot merge with a neighbor or land in a comment or a member access.
/// </summary>
static class JavaSpellingContext
{
    /// <summary>
    /// Validates every spelling span of the complete file text.
    /// </summary>
    /// <param name="text">The raw file text.</param>
    /// <param name="spans">The raw start (inclusive) and end (exclusive) of each spelling.</param>
    /// <exception cref="LanguagePackRefusal">A spelling touches an identifier character, follows a dot or a comment, or shares a line with a comment opener.</exception>
    internal static void Validate(string text, IReadOnlyList<(int Start, int End)> spans)
    {
        if (spans.Count == 0)
        {
            return;
        }

        var (decoded, map) = JavaUnicodeEscapes.DecodeWithMap(text);
        foreach (var (start, end) in spans)
        {
            if (Merges(text, start - 1) || Merges(text, end))
            {
                Refuse("touches an identifier character, a backslash or a non-ASCII character");
            }

            var index = Array.BinarySearch(map, start);
            CheckBefore(decoded, index < 0 ? ~index : index);
        }
    }

    static bool Merges(string text, int index) =>
        index >= 0 && index < text.Length && (char.IsAsciiLetterOrDigit(text[index]) || text[index] is '_' or '$' or '\\' || text[index] > '\u007f');

    static void CheckBefore(string decoded, int index)
    {
        var previous = index - 1;
        while (previous >= 0 && decoded[previous] is ' ' or '\t' or '\f' or '\r' or '\n')
        {
            previous--;
        }

        if (previous < 0)
        {
            return;
        }

        if (decoded[previous] is '.' or '/')
        {
            Refuse("follows a dot or a comment end");
        }

        var lineStart = previous == 0 ? 0 : decoded.LastIndexOfAny(['\r', '\n'], previous - 1) + 1;
        var line = decoded.AsSpan(lineStart, previous - lineStart);
        if (line.Contains("//", StringComparison.Ordinal) || line.Contains("/*", StringComparison.Ordinal))
        {
            Refuse("follows a comment opener on its line");
        }
    }

    static void Refuse(string reason) => throw new LanguagePackRefusal(JavaRefusalCodes.SpellingContext, $"A generated Java spelling {reason}.");
}
