// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Runtime;

/// <summary>
/// Adapts Chronicle's definition-language expressions to the pinned runtime's expression resolvers.
/// </summary>
internal static class ProjectionRuntimeExpression
{
    public static string Translate(string expression)
    {
        if (expression.StartsWith("$composite(", StringComparison.Ordinal))
        {
            var end = MatchingClose(expression, "$composite".Length);
            if (end != expression.Length - 1)
            {
                throw new UnsupportedProjectionRuntimeExpression(expression);
            }

            var arguments = expression["$composite(".Length..end];

            // The released kernel accepts only comma-delimited property=expression pairs. Commas inside
            // quotes/parentheses cannot be safely split by its resolver either, so reject those here.
            var parts = arguments.Split(',');
            var isNative = parts[0].Contains('=');
            if (!isNative && (parts.Length < 2 || !parts[0].Trim().All(character => char.IsLetterOrDigit(character) || character is '_')))
            {
                throw new UnsupportedProjectionRuntimeExpression(expression);
            }

            parts = isNative ? parts : parts[1..];
            if (parts.Any(part => part.Count(character => character == '=') != 1 || part.Contains('"') ||
                part.Count(character => character == '(') != part.Count(character => character == ')')))
            {
                throw new UnsupportedProjectionRuntimeExpression(expression);
            }

            return $"$composite({string.Join(", ", parts.Select(part =>
            {
                var pair = part.Trim().Split('=');
                return $"{pair[0].Trim()}={Translate(pair[1].Trim())}";
            }))})";
        }

        if (expression.StartsWith("$value(", StringComparison.Ordinal) || expression.StartsWith('"'))
        {
            // A constant is data, not a token to rewrite.
            return expression;
        }

        var result = new System.Text.StringBuilder();
        var quoted = false;
        for (var index = 0; index < expression.Length;)
        {
            if (expression[index] == '"' && (index == 0 || expression[index - 1] != '\\'))
            {
                quoted = !quoted;
                result.Append(expression[index++]);
            }
            else if (quoted)
            {
                result.Append(expression[index++]);
            }
            else if (expression[index] == '`')
            {
                var end = expression.IndexOf('`', index + 1);
                if (end < 0)
                {
                    throw new UnsupportedProjectionRuntimeExpression(expression);
                }

                var template = expression[(index + 1)..end];
                result.Append('`').Append(Template(template)).Append('`');
                index = end + 1;
            }
            else if (expression.AsSpan(index).StartsWith("$causedBy", StringComparison.Ordinal))
            {
                var tokenEnd = index + "$causedBy".Length;
                if (tokenEnd < expression.Length && expression[tokenEnd] == '(')
                {
                    var end = MatchingClose(expression, tokenEnd);
                    var property = expression[(tokenEnd + 1)..end];
                    if (property.Length == 0 || !property.All(character => char.IsLetterOrDigit(character) || character is '_' or '.'))
                    {
                        throw new UnsupportedProjectionRuntimeExpression(expression);
                    }

                    result.Append("$eventContext(causedBy.").Append(property).Append(')');
                    index = end + 1;
                }
                else if (tokenEnd == expression.Length ||
                    (!char.IsLetterOrDigit(expression[tokenEnd]) && expression[tokenEnd] is not ('_' or '.')))
                {
                    result.Append("$eventContext(causedBy)");
                    index = tokenEnd;
                }
                else
                {
                    throw new UnsupportedProjectionRuntimeExpression(expression);
                }
            }
            else
            {
                result.Append(expression[index++]);
            }
        }

        if (quoted)
        {
            throw new UnsupportedProjectionRuntimeExpression(expression);
        }

        return result.ToString();
    }

    static string Template(string source)
    {
        var result = new System.Text.StringBuilder();
        for (var index = 0; index < source.Length;)
        {
            if (source.AsSpan(index).StartsWith("${", StringComparison.Ordinal))
            {
                var end = source.IndexOf('}', index + 2);
                if (end < 0)
                {
                    throw new UnsupportedProjectionRuntimeExpression(source);
                }

                result.Append("${").Append(Translate(source[(index + 2)..end])).Append('}');
                index = end + 1;
            }
            else
            {
                result.Append(source[index++]);
            }
        }

        return result.ToString();
    }

    static int MatchingClose(string expression, int opening)
    {
        var depth = 0;
        for (var index = opening; index < expression.Length; index++)
        {
            if (expression[index] == '(') depth++;
            if (expression[index] == ')' && --depth == 0) return index;
        }

        throw new UnsupportedProjectionRuntimeExpression(expression);
    }
}
