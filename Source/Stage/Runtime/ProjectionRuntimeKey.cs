// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Runtime;

/// <summary>
/// Limits whole key expressions to supported Chronicle resolver syntax before registration.
/// Their regex matches are unanchored; partial matches can otherwise silently route to another key.
/// </summary>
internal static class ProjectionRuntimeKey
{
    public static string Translate(string expression)
    {
        var translated = ProjectionRuntimeExpression.Translate(expression);
        if (translated.Length == 0 || translated == "*NotSet*" || translated == "$eventSourceId" ||
            IsProperty(translated) || IsContext(translated) || IsValue(translated) || IsComposite(translated))
        {
            return translated;
        }

        throw new UnsupportedProjectionRuntimeExpression(expression);
    }

    public static string Identity(string expression) => expression.StartsWith('$') ? Translate(expression) : expression;

    static bool IsProperty(string expression) => expression.Length > 0 && expression[0] is not '$' &&
        IsPath(expression, allowDigits: true);

    static bool IsContext(string expression) => expression.StartsWith("$eventContext(", StringComparison.Ordinal) &&
        expression.EndsWith(')') && IsContextPath(expression["$eventContext(".Length..^1]);

    static bool IsValue(string expression) => expression.StartsWith("$value(", StringComparison.Ordinal) &&
        expression.EndsWith(')') && expression["$value(".Length..^1].All(character =>
            char.IsLetterOrDigit(character) || character is '_' or ' ' or '.' or '/' or ':' or '*' or '+' or '-');

    static bool IsComposite(string expression)
    {
        if (!expression.StartsWith("$composite(", StringComparison.Ordinal) || !expression.EndsWith(')'))
        {
            return false;
        }

        var body = expression["$composite(".Length..^1];

        // This is the complete character class of the pinned composite resolver. In particular, a
        // hyphen is legal in $value itself but makes the *outer* composite resolver fail to match.
        if (!body.All(character => char.IsLetterOrDigit(character) || character is '_' or '=' or '$' or '(' or ')' or '.' or ',' or ' '))
        {
            return false;
        }

        var parts = body.Split(',');
        return parts.Length > 0 && parts.All(part =>
        {
            var separator = part.IndexOf('=');
            return separator > 0 && separator == part.LastIndexOf('=') &&
                IsPath(part[..separator].Trim(), allowDigits: true) && IsScalar(part[(separator + 1)..].Trim());
        });
    }

    static bool IsScalar(string expression) => expression == "$eventSourceId" || IsProperty(expression) || IsContext(expression) || IsValue(expression);

    static bool IsContextPath(string expression) => expression.Length > 0 &&
        expression.All(character => IsAsciiLetter(character) || character == '.') &&
        expression[0] != '.' && !expression.EndsWith('.') && !expression.Contains("..", StringComparison.Ordinal);

    static bool IsPath(string expression, bool allowDigits) => expression.Length > 0 &&
        (IsAsciiLetter(expression[0]) || expression[0] == '_') &&
        expression.All(character => IsAsciiLetter(character) || character is '_' or '.' ||
            (allowDigits && character is >= '0' and <= '9')) &&
        !expression.Contains("..", StringComparison.Ordinal) && !expression.EndsWith('.');

    static bool IsAsciiLetter(char character) => (character is >= 'A' and <= 'Z') || (character is >= 'a' and <= 'z');
}
