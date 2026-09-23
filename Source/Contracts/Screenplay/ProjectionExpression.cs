// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Stores projection expressions exactly as Chronicle's definition-language visitor does.
/// </summary>
internal static class ProjectionExpression
{
    public static string Value(ExpressionSyntax expression) => expression switch
    {
        PathExpressionSyntax path => path.Path,
        EventContextExpressionSyntax context => $"$eventContext({context.Path})",
        EventSourceIdExpressionSyntax => "$eventSourceId",
        CausedByExpressionSyntax causedBy => causedBy.Property is null ? "$causedBy" : $"$causedBy({causedBy.Property})",
        LiteralExpressionSyntax literal => Literal(literal.Value),
        TemplateExpressionSyntax template => Template(template),
        RawExpressionSyntax raw => raw.Text,
        _ => throw new UnsupportedProjectionConversion(expression.GetType().Name)
    };

    public static string Key(ExpressionSyntax expression) => expression is LiteralExpressionSyntax { Value: string text }
        ? $"$value({text})"
        : Value(expression);

    public static string Key(KeySyntax? key) => key switch
    {
        null => string.Empty,
        ExpressionKeySyntax expression => Key(expression.Expression),
        CompositeKeySyntax composite => $"$composite({composite.Type}, {string.Join(", ", composite.Parts.Select(part => $"{part.Property}={Key(part.Expression)}"))})",
        _ => throw new UnsupportedProjectionConversion(key.GetType().Name)
    };

    static string Literal(object? value) => value switch
    {
        null => string.Empty,
        string text => $"\"{text}\"",
        bool boolean => boolean.ToString(),
        double number => number.ToString(CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
    };

    static string Template(TemplateExpressionSyntax template)
    {
        var builder = new StringBuilder();
        foreach (var part in template.Parts)
        {
            switch (part)
            {
                case TemplateTextSyntax text:
                    builder.Append(text.Text);
                    break;
                case TemplateInterpolationSyntax interpolation:
                    builder.Append("${").Append(Value(interpolation.Expression)).Append('}');
                    break;
            }
        }

        return $"`{builder}`";
    }
}
