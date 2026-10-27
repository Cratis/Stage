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
    static readonly FrozenSet<string> _typeDeclarationKeywords = new[] { "class", "interface", "enum", "record" }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>
    /// Scans generated text and native bodies as one literal stream. Structured placeholders are
    /// separated by whitespace; comment and literal state is retained across text fragments.
    /// </summary>
    /// <param name="file">The file whose literal identifiers must be reserved.</param>
    /// <returns>Identifiers outside comments and literals in the file's literal stream.</returns>
    /// <exception cref="LanguagePackRefusal">A Unicode escape or a comment or literal is malformed.</exception>
    public static IReadOnlySet<string> Identifiers(SymbolFile file) => Identifiers(string.Concat(file.Segments.Select(segment => segment switch
    {
        TextSegment text => text.Text,
        NativeSegment native => native.Body,
        _ => " ",
    })));

    /// <summary>
    /// Scans a native body in one forward pass over the decoded source.
    /// </summary>
    /// <param name="body">The raw Java source, possibly containing eligible Unicode escapes.</param>
    /// <returns>The ordinal, case-sensitive identifiers outside comments and literals.</returns>
    /// <exception cref="LanguagePackRefusal">A Unicode escape or a comment or literal is malformed.</exception>
    public static IReadOnlySet<string> Identifiers(string body)
    {
        return Tokens(JavaUnicodeEscapes.Decode(body)).Where(token => token.Identifier).Select(token => token.Text).ToFrozenSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// Refuses type declarations authored in generated text rather than structured declaration segments.
    /// Comments and literals retain their lexical state across segments; native declarations only reserve names.
    /// </summary>
    /// <param name="file">The file whose generated declarations must be structured.</param>
    /// <exception cref="LanguagePackRefusal">Generated code declares an unstructured type or cannot be scanned safely.</exception>
    public static void ValidateDeclarations(SymbolFile file)
    {
        var parts = file.Segments.Select(segment => (Text: DeclarationText(segment), Generated: segment is TextSegment)).ToArray();
        var generated = parts.SelectMany(part => Enumerable.Repeat(part.Generated, part.Text.Length)).ToArray();
        var tokens = Tokens(string.Concat(parts.Select(part => part.Text))).Where(token => !string.IsNullOrWhiteSpace(token.Text)).ToArray();
        for (var index = 0; index + 1 < tokens.Length; index++)
        {
            var token = tokens[index];
            if (generated[token.Start] && IsTypeDeclaration(tokens, index))
            {
                throw new LanguagePackRefusal(JavaRefusalCodes.UnstructuredDeclaration, "Java type declarations in generated text must use DeclarationSegment.");
            }
        }
    }

    static string DeclarationText(SymbolSegment segment) => segment switch
    {
        TextSegment literal => JavaUnicodeEscapes.Decode(literal.Text),
        NativeSegment native => JavaUnicodeEscapes.Decode(native.Body),
        DeclarationSegment => " \0 ",
        _ => " __structured ",
    };

    static bool IsTypeDeclaration((string Text, int Start, bool Identifier)[] tokens, int index) =>
        _typeDeclarationKeywords.Contains(tokens[index].Text) && tokens[index + 1].Identifier &&
        (index == 0 || tokens[index - 1].Text != ".");

    static IEnumerable<(string Text, int Start, bool Identifier)> Tokens(string source)
    {
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
                var identifier = ReadIdentifier(source, position);
                yield return (identifier.Name, position, true);
                position = identifier.End;
            }
            else
            {
                yield return (source[position].ToString(), position, false);
                position++;
            }
        }
    }

    static (string Name, int End) ReadIdentifier(string source, int position)
    {
        var start = position;
        while (position < source.Length && Rune.TryGetRuneAt(source, position, out var rune) && IsPart(rune)) position += rune.Utf16SequenceLength;
        var name = string.Concat(source[start..position].EnumerateRunes().Where(value => !IsIgnorable(value)).Select(value => value.ToString()));

        return (name, position);
    }

    static bool IsIdentifierStart(string source, int position) => Rune.TryGetRuneAt(source, position, out var rune) && IsStart(rune);

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
