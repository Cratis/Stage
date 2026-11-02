// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Specifications;

/// <summary>
/// Mirrors Screenplay's concrete fixture text while its expression formatter is internal. The text is diagnostic
/// only, so an expression kind it does not print renders as a placeholder instead of aborting the run.
/// </summary>
/// <remarks>
/// Screenplay has no JSON. A structured value is an indented block in source; on one diagnostic line it reads in the
/// words of that block: <c language="screenplay">(member = value, ...)</c> for an object, <c language="screenplay">(item value, ...)</c> for a list, <c language="screenplay">none</c>
/// for an empty list and <c language="screenplay">empty</c> for an object without members - the same display the event model board uses.
/// </remarks>
internal static class SpecificationFixtureText
{
    internal static string Expression(ExpressionSyntax expression) => expression switch
    {
        LiteralExpressionSyntax literal => Literal(literal.Value),
        PathExpressionSyntax path => path.Path,
        ListExpressionSyntax list when !list.Items.Any() => "none",
        ListExpressionSyntax list => $"({string.Join(", ", list.Items.Select(item => $"item {Expression(item)}"))})",
        ObjectExpressionSyntax obj when !obj.Members.Any() => "empty",
        ObjectExpressionSyntax obj => $"({string.Join(", ", obj.Members.Select(member => $"{MemberName(member.Name)} = {Expression(member.Value)}"))})",
        _ => $"<{expression.GetType().Name}>"
    };

    static string MemberName(string name)
    {
        if (name == "item") return "@item";
        var identifier = name.Length > 0 && (char.IsLetter(name[0]) || name[0] == '_') && name.All(character => char.IsLetterOrDigit(character) || character == '_');
        return identifier ? name : Literal(name);
    }

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
