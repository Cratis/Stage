// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Globalization;
using System.Text;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Collects identifiers used or declared in native Java bodies after JLS 3.3 decoding.
/// This conservative lexer skips comments and literals, but does not infer declaration scopes.
/// Every identifier it returns is reserved before import shortening, including package-obscuring locals.
/// </summary>
public static class InlineBodyScanner
{
    /// <summary>
    /// Scans a native body in one forward pass over the decoded source.
    /// </summary>
    /// <param name="body">The raw Java source, possibly containing eligible Unicode escapes.</param>
    /// <returns>The ordinal, case-sensitive identifiers outside comments and literals.</returns>
    /// <exception cref="LanguagePackRefusal">A Unicode escape or a comment or literal is malformed.</exception>
    public static IReadOnlySet<string> Identifiers(string body)
    {
        var source = JavaUnicodeEscapes.Decode(body);
        var names = new HashSet<string>(StringComparer.Ordinal);
        var position = 0;
        while (position < source.Length)
        {
            if (source.AsSpan(position).StartsWith("//", StringComparison.Ordinal))
            {
                while (position < source.Length && source[position] is not ('\n' or '\r')) position++;
            }
            else if (source.AsSpan(position).StartsWith("/*", StringComparison.Ordinal))
            {
                position = SkipComment(source, position);
            }
            else if (source[position] is '"' or '\'')
            {
                position = SkipLiteral(source, position);
            }
            else if (IsIdentifierStart(source, position))
            {
                position = ReserveIdentifier(source, position, names);
            }
            else
            {
                position++;
            }
        }

        return names.ToFrozenSet(StringComparer.Ordinal);
    }

    static bool IsIdentifierStart(string source, int position) => Rune.TryGetRuneAt(source, position, out var rune) && IsStart(rune);

    static int ReserveIdentifier(string source, int position, HashSet<string> names)
    {
        var start = position;
        while (position < source.Length && Rune.TryGetRuneAt(source, position, out var rune) && IsPart(rune)) position += rune.Utf16SequenceLength;
        names.Add(string.Concat(source[start..position].EnumerateRunes().Where(value => !IsIgnorable(value)).Select(value => value.ToString())));

        return position;
    }

    static bool IsStart(Rune rune) => Rune.GetUnicodeCategory(rune) is UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or
        UnicodeCategory.TitlecaseLetter or UnicodeCategory.ModifierLetter or UnicodeCategory.OtherLetter or UnicodeCategory.LetterNumber or
        UnicodeCategory.CurrencySymbol or UnicodeCategory.ConnectorPunctuation;

    static bool IsPart(Rune rune) => IsStart(rune) || IsIgnorable(rune) || Rune.GetUnicodeCategory(rune) is UnicodeCategory.DecimalDigitNumber or
        UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark;

    static bool IsIgnorable(Rune rune) => Rune.GetUnicodeCategory(rune) == UnicodeCategory.Format ||
        rune.Value is (>= 0 and <= 8) or (>= 14 and <= 27) or (>= 127 and <= 159);

    static int SkipComment(string source, int position)
    {
        var end = source.IndexOf("*/", position + 2, StringComparison.Ordinal);
        if (end < 0) throw Unterminated("block comment");

        return end + 2;
    }

    static int SkipLiteral(string source, int position)
    {
        var quote = source[position];
        var textBlock = source.AsSpan(position).StartsWith("\"\"\"", StringComparison.Ordinal);
        position += textBlock ? 3 : 1;
        while (position < source.Length)
        {
            if (source[position] == '\\')
            {
                // Consume escape pairs. An odd backslash escapes a quote; an even run does not.
                position += 2;
            }
            else if (AtTerminator(source, position, textBlock, quote))
            {
                return position + (textBlock ? 3 : 1);
            }
            else if (!textBlock && source[position] is '\n' or '\r')
            {
                throw Unterminated("string or character literal");
            }
            else
            {
                position++;
            }
        }

        throw Unterminated(textBlock ? "text block" : "string or character literal");
    }

    static bool AtTerminator(string source, int position, bool textBlock, char quote) => textBlock
        ? source.AsSpan(position).StartsWith("\"\"\"", StringComparison.Ordinal)
        : source[position] == quote;

    static LanguagePackRefusal Unterminated(string construct) => new(JavaRefusalCodes.UnterminatedNativeConstruct, $"Unterminated Java {construct} in a native body.");
}
