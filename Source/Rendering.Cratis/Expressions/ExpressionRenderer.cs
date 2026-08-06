// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Expressions;

/// <summary>
/// Renders a Screenplay <see cref="ExpressionSyntax"/> or <see cref="ConditionSyntax"/> as C# expression text.
/// Shared by every renderer that emits an expression — command <c>produces</c> mappings, projection mappings,
/// and their guarding conditions all resolve through the same set of rules.
/// </summary>
/// <remarks>
/// Every rendered expression that reaches beyond the command/event's own properties (<c>$context.*</c>,
/// <c>$eventContext.*</c>, <c>$causedBy</c>, <c>$eventSourceId</c>) assumes the enclosing method declares its
/// context parameter as <c>context</c> — the same fixed name Screenplay's own authored <c>csharp</c> code blocks
/// assume (see <c>HandlerSyntax</c>/<c>ReactorTriggerSyntax</c> code blocks). Root-specific semantics of
/// <c>$context.&lt;root&gt;.*</c> beyond that are best-effort (PascalCase every path segment) since no confirmed
/// Arc API mapping exists for every root.
/// </remarks>
public static class ExpressionRenderer
{
    /// <summary>
    /// Renders an expression as C# expression text.
    /// </summary>
    /// <param name="expression">The expression to render.</param>
    /// <returns>The rendered C# expression text.</returns>
    /// <exception cref="UnsupportedExpression">Thrown when the expression has no C# rendering.</exception>
    public static string Render(ExpressionSyntax expression) => expression switch
    {
        LiteralExpressionSyntax literal => RenderLiteral(literal.Value),
        PathExpressionSyntax path => RenderPath(path.Path),
        SourceItemExpressionSyntax sourceItem => RenderPath(sourceItem.Path),
        ContextExpressionSyntax context => RenderContextPath(context.Path),
        EnvironmentExpressionSyntax environment => $"Environment.GetEnvironmentVariable(\"{Escape(environment.Name)}\")",
        SecretExpressionSyntax secret =>
            $"Environment.GetEnvironmentVariable(\"{Escape(secret.Name)}\") /* TODO: resolve '{secret.Name}' from secrets, not environment */",
        StringsExpressionSyntax strings => $"\"{Escape(strings.Key)}\" /* TODO: resolve localized string */",
        RawExpressionSyntax raw => raw.Text,
        EventSourceIdExpressionSyntax => "context.EventSourceId",
        EventContextExpressionSyntax eventContext => RenderContextPath(eventContext.Path),
        CausedByExpressionSyntax causedBy => causedBy.Property is null
            ? "context.CausedBy"
            : $"context.CausedBy.{Identifiers.ToPascalCase(causedBy.Property)}",
        TemplateExpressionSyntax template => RenderTemplate(template),
        _ => throw new UnsupportedExpression(expression),
    };

    /// <summary>
    /// Renders a condition as a C# boolean expression.
    /// </summary>
    /// <param name="condition">The condition to render.</param>
    /// <param name="enumTypeOf">
    /// Resolves the enum type name of a path being compared, when it has one. A string literal compared against an
    /// enum-typed value has to be rendered as the enum member — the literal is what the Screenplay author wrote,
    /// but the two types have no comparison operator between them.
    /// </param>
    /// <returns>The rendered C# boolean expression text.</returns>
    /// <exception cref="UnsupportedCondition">Thrown when the condition has no C# rendering.</exception>
    public static string Render(ConditionSyntax condition, Func<string, string?>? enumTypeOf = null) => condition switch
    {
        ComparisonConditionSyntax comparison =>
            $"{RenderPath(comparison.Left)} {Operator(comparison.Operator)} {RenderComparand(comparison, enumTypeOf)}",
        LogicalConditionSyntax logical =>
            $"({Render(logical.Left, enumTypeOf)}) {Operator(logical.Operator)} ({Render(logical.Right, enumTypeOf)})",
        _ => throw new UnsupportedCondition(condition),
    };

    static string RenderComparand(ComparisonConditionSyntax comparison, Func<string, string?>? enumTypeOf) =>
        comparison.Right is LiteralExpressionSyntax { Value: string text } && enumTypeOf?.Invoke(comparison.Left) is { } enumName
            ? $"{enumName}.{Identifiers.ToPascalCase(text)}"
            : Render(comparison.Right);

    static string RenderLiteral(object? value) => value switch
    {
        null => "null",
        bool boolean => boolean ? "true" : "false",
        string text => $"\"{Escape(text)}\"",
        double number => number.ToString(CultureInfo.InvariantCulture),
        decimal number => number.ToString(CultureInfo.InvariantCulture),
        int number => number.ToString(CultureInfo.InvariantCulture),
        long number => number.ToString(CultureInfo.InvariantCulture),
        _ => $"\"{Escape(value.ToString() ?? string.Empty)}\"",
    };

    static string RenderPath(string path) => string.Join('.', path.Split('.').Select(Identifiers.ToPascalCase));

    static string RenderContextPath(string path) => $"context.{RenderPath(path)}";

    static string RenderTemplate(TemplateExpressionSyntax template)
    {
        var builder = new StringBuilder("$\"");
        foreach (var part in template.Parts)
        {
            if (part is TemplateTextSyntax text)
            {
                builder.Append(EscapeInterpolated(text.Text));
            }
            else if (part is TemplateInterpolationSyntax interpolation)
            {
                builder.Append('{').Append(Render(interpolation.Expression)).Append('}');
            }
        }

        return builder.Append('"').ToString();
    }

    static string Operator(ComparisonOperator @operator) => @operator switch
    {
        ComparisonOperator.Equal => "==",
        ComparisonOperator.NotEqual => "!=",
        ComparisonOperator.GreaterThan => ">",
        ComparisonOperator.GreaterThanOrEqual => ">=",
        ComparisonOperator.LessThan => "<",
        ComparisonOperator.LessThanOrEqual => "<=",
        _ => "==",
    };

    static string Operator(LogicalOperator @operator) => @operator == LogicalOperator.Or ? "||" : "&&";

    static string Escape(string text) => text.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    static string EscapeInterpolated(string text) =>
        Escape(text).Replace("{", "{{", StringComparison.Ordinal).Replace("}", "}}", StringComparison.Ordinal);
}
