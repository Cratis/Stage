// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Lexes a complete Java 21 file after eligibility-aware Unicode translation, retaining raw-source spans.
/// </summary>
/// <remarks>
/// Only CR, LF and CRLF terminate line comments. Only space, HT, FF, CR and LF are Java whitespace.
/// Strings, character literals and text blocks are opaque tokens; Java 21 preview string templates are
/// not supported. This lexer is not itself evidence of lexical containment or a type binding.
/// </remarks>
public static class JavaSourceLexer
{
    /// <summary>
    /// Tokenizes raw source once and maps tokens through JLS 3.3 translation.
    /// </summary>
    /// <param name="raw">The entire emitted file.</param>
    /// <returns>Code and literal tokens, excluding comments and Java whitespace.</returns>
    /// <exception cref="LanguagePackRefusal">Translation or a comment or literal is malformed.</exception>
    public static IReadOnlyList<JavaSourceToken> Scan(string raw)
    {
        var starts = new List<int>();
        var ends = new List<int>();
        var source = JavaUnicodeEscapes.Decode(raw, (start, end) => { starts.Add(start); ends.Add(end); });
        var tokens = new List<JavaSourceToken>();
        var position = 0;
        while (position < source.Length)
        {
            var start = position;
            if (SkipTrivia(source, ref position)) continue;
            var literal = source[position] is '"' or '\'';
            var identifier = IsStart(source, position);
            position = End(source, position, literal, identifier);
            var text = source[start..position];
            if (identifier) text = string.Concat(text.EnumerateRunes().Where(rune => !IsIgnorable(rune)).Select(rune => rune.ToString()));
            tokens.Add(new(text, starts[start], ends[position - 1], identifier, literal));
        }

        return tokens;
    }

    static bool SkipTrivia(string source, ref int position)
    {
        if (source[position] is ' ' or '\t' or '\f' or '\r' or '\n') { position++; return true; }
        if (source.AsSpan(position).StartsWith("//", StringComparison.Ordinal))
        {
            while (position < source.Length && source[position] is not ('\r' or '\n')) position++;
            return true;
        }

        if (!source.AsSpan(position).StartsWith("/*", StringComparison.Ordinal)) return false;
        var end = source.IndexOf("*/", position + 2, StringComparison.Ordinal);
        if (end < 0) throw Unterminated();
        position = end + 2;
        return true;
    }

    static int End(string source, int position, bool literal, bool identifier)
    {
        if (literal) return LiteralEnd(source, position);
        if (!identifier)
        {
            return source.AsSpan(position).StartsWith("->", StringComparison.Ordinal) || source.AsSpan(position).StartsWith("::", StringComparison.Ordinal) ? position + 2 : position + 1;
        }

        while (position < source.Length && Rune.TryGetRuneAt(source, position, out var rune) && IsPart(rune)) position += rune.Utf16SequenceLength;
        return position;
    }

    static int LiteralEnd(string source, int position)
    {
        var quote = source[position];
        var block = source.AsSpan(position).StartsWith("\"\"\"", StringComparison.Ordinal);
        position += block ? 3 : 1;
        while (position < source.Length)
        {
            if (source[position] == '\\') { position += 2; continue; }
            if (block ? source.AsSpan(position).StartsWith("\"\"\"", StringComparison.Ordinal) : source[position] == quote) return position + (block ? 3 : 1);
            if (!block && source[position] is '\r' or '\n') throw Unterminated();
            position++;
        }

        throw Unterminated();
    }

    static bool IsStart(string source, int position) => Rune.TryGetRuneAt(source, position, out var rune) && IsStart(rune);

    static bool IsStart(Rune rune) => Rune.GetUnicodeCategory(rune) is UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or
        UnicodeCategory.TitlecaseLetter or UnicodeCategory.ModifierLetter or UnicodeCategory.OtherLetter or UnicodeCategory.LetterNumber or
        UnicodeCategory.CurrencySymbol or UnicodeCategory.ConnectorPunctuation;

    static bool IsPart(Rune rune) => IsStart(rune) || IsIgnorable(rune) || Rune.GetUnicodeCategory(rune) is UnicodeCategory.DecimalDigitNumber or
        UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark;

    static bool IsIgnorable(Rune rune) => Rune.GetUnicodeCategory(rune) == UnicodeCategory.Format ||
        rune.Value is (>= 0 and <= 8) or (>= 14 and <= 27) or (>= 127 and <= 159);

    static LanguagePackRefusal Unterminated() => new(JavaRefusalCodes.UnterminatedNativeConstruct, "An emitted Java comment or literal is unterminated.");
}
