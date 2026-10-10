// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Specifications;

/// <summary>
/// Mirrors Screenplay's concrete fixture text while its expression formatter is internal. The text is diagnostic
/// only, so an expression kind it does not print renders as a placeholder instead of aborting the run.
/// </summary>
internal static class SpecificationFixtureText
{
    static readonly JsonSerializerOptions _structuredOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    internal static string Expression(ExpressionSyntax expression) => expression switch
    {
        LiteralExpressionSyntax literal => Literal(literal.Value),
        PathExpressionSyntax path => path.Path,
        ListExpressionSyntax list => $"[{string.Join(',', list.Items.Select(Structured))}]",
        ObjectExpressionSyntax obj => $"{{{string.Join(',', obj.Members.Select(member => $"{JsonSerializer.Serialize(member.Name, _structuredOptions)}:{Structured(member.Value)}"))}}}",
        _ => $"<{expression.GetType().Name}>"
    };

    static string Structured(ExpressionSyntax expression) => expression is LiteralExpressionSyntax { Value: string text }
        ? JsonSerializer.Serialize(text, _structuredOptions)
        : Expression(expression);

    static string Literal(object? value) => value switch
    {
        null => "null",
        bool boolean => boolean ? "true" : "false",
        string text => $"\"{text.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal).Replace("\r", "\\r", StringComparison.Ordinal).Replace("\t", "\\t", StringComparison.Ordinal)}\"",
        ExactNumber number => number.CanonicalText,
        double number => Number(number),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    static string Number(double number)
    {
        if (number == Math.Floor(number) && number >= long.MinValue && number < 9223372036854775808d)
        {
            return ((long)number).ToString(CultureInfo.InvariantCulture);
        }

        var text = number.ToString("R", CultureInfo.InvariantCulture);
        var exponentAt = text.IndexOf('E');
        if (exponentAt < 0) return text;
        var negative = text[0] == '-';
        var mantissa = text[(negative ? 1 : 0)..exponentAt];
        var point = mantissa.IndexOf('.');
        var digits = mantissa.Replace(".", string.Empty, StringComparison.Ordinal);
        var decimalAt = (point < 0 ? mantissa.Length : point) + int.Parse(text[(exponentAt + 1)..], CultureInfo.InvariantCulture);
        var expanded = decimalAt switch
        {
            <= 0 => $"0.{new string('0', -decimalAt)}{digits}",
            _ when decimalAt >= digits.Length => digits + new string('0', decimalAt - digits.Length),
            _ => digits.Insert(decimalAt, ".")
        };

        return negative ? "-" + expanded : expanded;
    }
}
