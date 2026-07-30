// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Converts Screenplay <see cref="ExpressionSyntax"/> and <see cref="KeySyntax"/> nodes into the Chronicle-compatible
/// projection expression strings Stage's <c>ProjectionDefinition</c> carries (for example <c>$eventSourceId</c>,
/// <c>$eventContext(occurred)</c>, <c>$value("global")</c>). Targets the expressions the projection engine's
/// resolvers support — which is narrower than what Chronicle's own definition-language visitor emits.
/// </summary>
public static class ScreenplayExpression
{
    /// <summary>
    /// Converts an expression into its Chronicle projection expression string.
    /// </summary>
    /// <param name="expression">The expression to convert.</param>
    /// <returns>The Chronicle-compatible expression string.</returns>
    public static string ToProjectionExpression(ExpressionSyntax expression) =>
        expression switch
        {
            PathExpressionSyntax path => path.Path,
            EventContextExpressionSyntax eventContext => $"$eventContext({eventContext.Path})",
            EventSourceIdExpressionSyntax => "$eventSourceId",

            // Chronicle's projection engine has no resolver for the $causedBy token — but the identity that
            // caused an event lives on the event context, so $causedBy translates to an event context path.
            CausedByExpressionSyntax causedBy => causedBy.Property is null ? "$eventContext(causedBy)" : $"$eventContext(causedBy.{causedBy.Property})",
            LiteralExpressionSyntax literal => FormatLiteral(literal.Value),
            TemplateExpressionSyntax template => FormatTemplate(template),
            RawExpressionSyntax raw => raw.Text,
            ContextExpressionSyntax context => $"$context.{context.Path}",
            EnvironmentExpressionSyntax environment => $"$env.{environment.Name}",
            SourceItemExpressionSyntax sourceItem => $"$.{sourceItem.Path}",
            _ => string.Empty
        };

    /// <summary>
    /// Converts an expression used as a key into its Chronicle key expression string. String literals are wrapped in
    /// the <c>$value(...)</c> token so the engine treats them as constant keys rather than property references.
    /// </summary>
    /// <param name="expression">The key expression to convert.</param>
    /// <returns>The Chronicle-compatible key expression string.</returns>
    public static string ToKeyExpression(ExpressionSyntax expression) =>
        expression switch
        {
            LiteralExpressionSyntax { Value: string value } => $"$value({value})",
            _ => ToProjectionExpression(expression)
        };

    /// <summary>
    /// Converts a key node into its Chronicle key expression string, or <see langword="null"/> when no key is declared
    /// (letting the runtime default the key to the event source id).
    /// </summary>
    /// <param name="key">The key node, or <see langword="null"/>.</param>
    /// <returns>The Chronicle-compatible key expression string, or <see langword="null"/>.</returns>
    public static string? ToKeyExpression(KeySyntax? key) =>
        key switch
        {
            null => null,
            ExpressionKeySyntax expressionKey => ToKeyExpression(expressionKey.Expression),

            // The engine's $composite(...) grammar is strictly comma-separated property=expression pairs — the
            // Screenplay-declared key type name has no slot in it and must not be emitted.
            CompositeKeySyntax composite => $"$composite({string.Join(", ", composite.Parts.Select(part => $"{part.Property}={ToKeyExpression(part.Expression)}"))})",
            _ => null
        };

    /// <summary>
    /// Converts an expression into the JSON value it represents, used when serializing specification property values.
    /// Literals become their JSON value; every other expression becomes its string form.
    /// </summary>
    /// <param name="expression">The expression to convert.</param>
    /// <returns>The JSON node for the value.</returns>
    public static JsonNode? ToJsonValue(ExpressionSyntax expression) =>
        expression switch
        {
            LiteralExpressionSyntax { Value: null } => null,
            LiteralExpressionSyntax { Value: string text } => JsonValue.Create(text),
            LiteralExpressionSyntax { Value: bool boolean } => JsonValue.Create(boolean),
            LiteralExpressionSyntax { Value: double number } => JsonValue.Create(number),
            LiteralExpressionSyntax literal => JsonValue.Create(Convert.ToString(literal.Value, CultureInfo.InvariantCulture)),
            _ => JsonValue.Create(ToProjectionExpression(expression))
        };

    static string FormatLiteral(object? value) =>
        value switch
        {
            null => string.Empty,
            string text => $"\"{text}\"",
            bool boolean => boolean.ToString(),
            double number => number.ToString(CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };

    static string FormatTemplate(TemplateExpressionSyntax template)
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
                    builder.Append("${").Append(ToProjectionExpression(interpolation.Expression)).Append('}');
                    break;
            }
        }

        return $"`{builder}`";
    }
}
