// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using Scriban.Parsing;

namespace Cratis.Stage.Rendering.Templates;

internal static class TemplatePreScan
{
    internal const int MaxBytes = 65536;
    internal const int MaxDepth = 32;
    static readonly HashSet<string> _openers = new(["if", "for", "while", "func", "capture", "case", "with", "wrap", "tablerow"], StringComparer.Ordinal);

    // The pinned nonrecursive lexer distinguishes strings, comments and escaped raw text before any parser recursion.
    internal static void Check(string path, string text, bool expression)
    {
        if (Encoding.UTF8.GetByteCount(text) > MaxBytes)
        {
            throw new TemplateRefusal(new(TemplateCodes.TemplateSize, "Template exceeds 65536 UTF-8 bytes.", path, 0, 0));
        }

        CheckPrivateUse(path, text);
        var blocks = 0;
        var brackets = 0;
        var unary = 0;
        var previous = string.Empty;
        var options = new LexerOptions { Mode = expression ? ScriptMode.ScriptOnly : ScriptMode.Default };
        foreach (var token in new Lexer(text, path, options))
        {
            var word = token.Type == TokenType.Identifier ? token.GetText(text) : string.Empty;
            if (token.Type is TokenType.OpenParen or TokenType.OpenBracket or TokenType.OpenBrace) brackets++;
            else if (token.Type is TokenType.CloseParen or TokenType.CloseBracket or TokenType.CloseBrace) brackets = Math.Max(0, brackets - 1);
            else if (token.Type is TokenType.Exclamation or TokenType.Minus or TokenType.Plus) unary++;
            else if (token.Type is not (TokenType.Whitespace or TokenType.NewLine or TokenType.Comment or TokenType.CommentMulti)) unary = 0;

            if (word == "end") blocks = Math.Max(0, blocks - 1);
            else if (previous != "else" && _openers.Contains(word)) blocks++;
            if (blocks + brackets + unary > MaxDepth)
            {
                throw new TemplateRefusal(new(TemplateCodes.Nesting, "Template nesting exceeds 32.", path, token.Start.Line + 1, token.Start.Column + 1));
            }

            if (token.Type != TokenType.Whitespace) previous = word;
        }
    }

    internal static bool HasPrivateUse(string text) => text.EnumerateRunes().Any(rune => Rune.GetUnicodeCategory(rune) == UnicodeCategory.PrivateUse);

    static void CheckPrivateUse(string path, string text)
    {
        var line = 1;
        var column = 1;
        foreach (var rune in text.EnumerateRunes())
        {
            if (Rune.GetUnicodeCategory(rune) == UnicodeCategory.PrivateUse)
            {
                throw new TemplateRefusal(new(TemplateCodes.TemplatePrivateUse, "Private-use code point in template.", path, line, column));
            }

            if (rune.Value == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column += rune.Utf16SequenceLength;
            }
        }
    }
}
