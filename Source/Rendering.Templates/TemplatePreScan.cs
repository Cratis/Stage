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
    static readonly HashSet<string> _openers = new(["if", "for", "while", "do", "func", "capture", "case", "with", "wrap", "tablerow"], StringComparer.Ordinal);

    // The pinned nonrecursive lexer distinguishes strings, comments and escaped raw text before any parser recursion.
    internal static void Check(string path, string text, bool expression)
    {
        if (Encoding.UTF8.GetByteCount(text) > MaxBytes)
        {
            throw new TemplateRefusal(new(TemplateCodes.TemplateSize, "Template exceeds 65536 UTF-8 bytes.", path, 0, 0));
        }

        CheckPrivateUse(path, text);
        var nesting = new Nesting(text);
        var options = new LexerOptions { Mode = expression ? ScriptMode.ScriptOnly : ScriptMode.Default };
        foreach (var token in new Lexer(text, path, options))
        {
            nesting.Advance(token);
            if (nesting.Depth > MaxDepth)
            {
                throw new TemplateRefusal(new(TemplateCodes.Nesting, "Template nesting exceeds 32.", path, token.Start.Line + 1, token.Start.Column + 1));
            }
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

    sealed class Nesting(string text)
    {
        int _blocks;
        int _brackets;
        int _unary;
        bool _canCloseBlock = true;
        TokenType _previous = TokenType.Invalid;

        internal int Depth => _blocks + _brackets + _unary;

        internal void Advance(Token token)
        {
            CountBrackets(token.Type);
            CountUnary(token.Type);
            CountBlocks(token);
            _canCloseBlock = CanCloseBlock(token.Type);
            if (!IsTrivia(token.Type)) _previous = token.Type;
        }

        static bool IsTrivia(TokenType token) => token is TokenType.Whitespace or TokenType.NewLine or TokenType.Comment or TokenType.CommentMulti;

        void CountBrackets(TokenType token) => _brackets = token switch
        {
            TokenType.OpenParen or TokenType.OpenBracket or TokenType.OpenBrace => _brackets + 1,
            TokenType.CloseParen or TokenType.CloseBracket or TokenType.CloseBrace => Math.Max(0, _brackets - 1),
            _ => _brackets
        };

        void CountUnary(TokenType token) => _unary = token switch
        {
            TokenType.Exclamation or TokenType.Minus or TokenType.Plus => _unary + 1,
            TokenType.OpenParen or TokenType.OpenBracket or TokenType.OpenBrace or TokenType.CloseParen or TokenType.CloseBracket or TokenType.CloseBrace => _unary,
            _ when IsTrivia(token) => _unary,
            _ => 0
        };

        // Count every possible opener, including expression-position functions and blocks inside brackets.
        // Only ends after hard statement boundaries outside brackets reduce the bound; a newline may continue an expression.
        // Member names, strings, raw text and comments cannot alter it. Ambiguity must only over-count toward
        // STAGE-TPL-003, never hide nesting from the recursive parser.
        void CountBlocks(Token token)
        {
            var word = BlockWord(token);
            _blocks = word switch
            {
                "end" when _canCloseBlock => Math.Max(0, _blocks - 1),
                _ when _openers.Contains(word) => _blocks + 1,
                _ => _blocks
            };
        }

        string BlockWord(Token token) =>
            token.Type == TokenType.Identifier && _previous is not (TokenType.Dot or TokenType.QuestionDot) ? token.GetText(text) : string.Empty;

        bool CanCloseBlock(TokenType token) => token switch
        {
            TokenType.CodeEnter or TokenType.SemiColon when _brackets == 0 => true,
            TokenType.Whitespace or TokenType.NewLine or TokenType.Comment or TokenType.CommentMulti => _canCloseBlock,
            _ => false
        };
    }
}
