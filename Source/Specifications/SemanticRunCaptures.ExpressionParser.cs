// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Specifications;

// Capture guard grammar ported from Screenplay v4.240.2; Stage owns evaluation.
internal static partial class SemanticRunCaptureExpression
{
    sealed class Parser(string text)
    {
        static readonly string[] _operators = ["==", "!=", "<=", ">=", "<", ">"];
        int _position;
        int _depth;

        public bool TryParse(out Node expression)
        {
            expression = new Literal(SemanticValue.Null);
            var parsed = ParseOr();
            SkipWhitespace();
            if (parsed is null || _position != text.Length)
            {
                return false;
            }

            expression = parsed;
            return true;
        }

        Node? ParseOr()
        {
            var left = ParseAnd();
            while (left is not null && Take("||"))
            {
                var right = ParseAnd();
                left = right is null ? null : new Or(left, right);
            }

            return left;
        }

        Node? ParseAnd()
        {
            var left = ParseUnary();
            while (left is not null && Take("&&"))
            {
                var right = ParseUnary();
                left = right is null ? null : new And(left, right);
            }

            return left;
        }

        Node? ParseUnary()
        {
            if (++_depth > 32)
            {
                return null;
            }

            try
            {
                if (Peek("!") && !Peek("!="))
                {
                    _position = SkipWhitespace() + 1;
                    return ParseUnary() is { } operand ? new Not(operand) : null;
                }

                if (Take("("))
                {
                    var inner = ParseOr();
                    return inner is not null && Take(")") ? inner : null;
                }

                var left = ParseOperand();
                if (left is null)
                {
                    return null;
                }

                foreach (var candidate in _operators)
                {
                    if (Take(candidate))
                    {
                        return ParseOperand() is { } right ? new Comparison(left, candidate, right) : null;
                    }
                }

                return left;
            }
            finally
            {
                _depth--;
            }
        }

        Node? ParseOperand()
        {
            var start = SkipWhitespace();
            if (start >= text.Length)
            {
                return null;
            }

            if (text[start] == '"')
            {
                var end = text.IndexOf('"', start + 1);
                if (end < 0)
                {
                    return null;
                }

                _position = end + 1;
                return new Literal(SemanticValue.Text(text[(start + 1)..end]));
            }

            var position = start;
            while (position < text.Length && (char.IsLetterOrDigit(text[position]) || text[position] is '_' or '.' or '-' or '$'))
            {
                position++;
            }

            if (position == start)
            {
                return null;
            }

            _position = position;
            var word = text[start..position];
            if (string.Equals(word, "true", StringComparison.Ordinal) || string.Equals(word, "false", StringComparison.Ordinal))
            {
                return new Literal(SemanticValue.Boolean(string.Equals(word, "true", StringComparison.Ordinal)));
            }

            if (string.Equals(word, "null", StringComparison.Ordinal))
            {
                return new Literal(SemanticValue.Null);
            }

            if (char.IsDigit(word[0]) || word[0] == '-')
            {
                return decimal.TryParse(word, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number)
                    ? new Literal(SemanticValue.Number(number))
                    : null;
            }

            return word.StartsWith("$.", StringComparison.Ordinal) ? new Field(word[2..]) : new Field(word);
        }

        bool Peek(string token)
        {
            var start = SkipWhitespace();
            return string.CompareOrdinal(text, start, token, 0, token.Length) == 0;
        }

        bool Take(string token)
        {
            if (!Peek(token))
            {
                return false;
            }

            _position = SkipWhitespace() + token.Length;
            return true;
        }

        int SkipWhitespace()
        {
            while (_position < text.Length && char.IsWhiteSpace(text[_position]))
            {
                _position++;
            }

            return _position;
        }
    }
}
