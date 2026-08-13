// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Expressions;

/// <summary>
/// Renders a Screenplay <see cref="ExpressionSyntax"/> or <see cref="ConditionSyntax"/> as C# expression text.
/// Shared by every renderer that emits an expression — command <c>produces</c> mappings, projection mappings,
/// and their guarding conditions all resolve through the same set of rules.
/// </summary>
/// <remarks>
/// An expression that reaches beyond the artifact's own properties (<c>$context.*</c>, <c>$eventContext.*</c>,
/// <c>$causedBy</c>, <c>$eventSourceId</c>) has no single C# rendering — what it becomes depends on what the
/// enclosing artifact receives. The overloads taking an <see cref="IExpressionContext"/> let the caller say;
/// the ones without assume Chronicle's <c>EventContext</c> is in scope as <c>context</c>, which holds for a
/// reactor method and a projection and for nothing else.
/// </remarks>
public static class ExpressionRenderer
{
    /// <summary>
    /// Renders an expression as C# expression text, against Chronicle's <c>EventContext</c>.
    /// </summary>
    /// <param name="expression">The expression to render.</param>
    /// <returns>The rendered C# expression text.</returns>
    /// <exception cref="UnsupportedExpression">Thrown when the expression has no C# rendering.</exception>
    public static string Render(ExpressionSyntax expression) => Render(expression, EventContextAccess.Instance);

    /// <summary>
    /// Renders an expression as C# expression text, against the surroundings the enclosing artifact provides.
    /// </summary>
    /// <param name="expression">The expression to render.</param>
    /// <param name="context">The <see cref="IExpressionContext"/> rendering what reaches outside the artifact.</param>
    /// <returns>The rendered C# expression text.</returns>
    /// <exception cref="UnsupportedExpression">Thrown when the expression has no C# rendering.</exception>
    public static string Render(ExpressionSyntax expression, IExpressionContext context) => expression switch
    {
        LiteralExpressionSyntax literal => RenderLiteral(literal.Value),
        PathExpressionSyntax path => RenderPath(path.Path),
        SourceItemExpressionSyntax sourceItem => RenderPath(sourceItem.Path),
        ContextExpressionSyntax contextExpression => context.Render(contextExpression),
        EnvironmentExpressionSyntax environment => $"Environment.GetEnvironmentVariable({CSharpCodeBuilder.StringLiteral(environment.Name)})",
        StringsExpressionSyntax strings =>$"{CSharpCodeBuilder.StringLiteral(strings.Key)} /* TODO: resolve localized string */",
        RawExpressionSyntax raw => raw.Text,
        EventSourceIdExpressionSyntax => context.RenderEventSourceId(),
        EventContextExpressionSyntax eventContext => context.Render(eventContext),
        CausedByExpressionSyntax causedBy => context.Render(causedBy),
        TemplateExpressionSyntax template => RenderTemplate(template, context),
        _ => throw new UnsupportedExpression(expression),
    };

    /// <summary>
    /// Renders a condition as a C# boolean expression, against Chronicle's <c>EventContext</c>.
    /// </summary>
    /// <param name="condition">The condition to render.</param>
    /// <param name="enumTypeOf">
    /// Resolves the enum type name of a path being compared, when it has one. A string literal compared against an
    /// enum-typed value has to be rendered as the enum member — the literal is what the Screenplay author wrote,
    /// but the two types have no comparison operator between them.
    /// </param>
    /// <returns>The rendered C# boolean expression text.</returns>
    /// <exception cref="UnsupportedCondition">Thrown when the condition has no C# rendering.</exception>
    public static string Render(ConditionSyntax condition, Func<string, string?>? enumTypeOf = null) =>
        Render(condition, EventContextAccess.Instance, enumTypeOf);

    /// <summary>
    /// Renders a condition as a C# boolean expression, against the surroundings the enclosing artifact provides.
    /// </summary>
    /// <param name="condition">The condition to render.</param>
    /// <param name="context">The <see cref="IExpressionContext"/> rendering what reaches outside the artifact.</param>
    /// <param name="enumTypeOf">Resolves the enum type name of a path being compared, when it has one.</param>
    /// <returns>The rendered C# boolean expression text.</returns>
    /// <exception cref="UnsupportedCondition">Thrown when the condition has no C# rendering.</exception>
    public static string Render(ConditionSyntax condition, IExpressionContext context, Func<string, string?>? enumTypeOf = null) => condition switch
    {
        ComparisonConditionSyntax comparison =>
            $"{RenderPath(comparison.Left)} {Operator(comparison.Operator)} {RenderComparand(comparison, context, enumTypeOf)}",
        LogicalConditionSyntax logical =>
            $"({Render(logical.Left, context, enumTypeOf)}) {Operator(logical.Operator)} ({Render(logical.Right, context, enumTypeOf)})",
        _ => throw new UnsupportedCondition(condition),
    };

    static string RenderComparand(ComparisonConditionSyntax comparison, IExpressionContext context, Func<string, string?>? enumTypeOf) =>
        comparison.Right is LiteralExpressionSyntax { Value: string text } && enumTypeOf?.Invoke(comparison.Left) is { } enumName
            ? $"{enumName}.{Identifiers.ToPascalCase(text)}"
            : Render(comparison.Right, context);

    static string RenderLiteral(object? value) => value switch
    {
        null => "null",
        bool boolean => boolean ? "true" : "false",
        string text => CSharpCodeBuilder.StringLiteral(text),
        double number => number.ToString(CultureInfo.InvariantCulture),
        decimal number => number.ToString(CultureInfo.InvariantCulture),
        int number => number.ToString(CultureInfo.InvariantCulture),
        long number => number.ToString(CultureInfo.InvariantCulture),
        _ => CSharpCodeBuilder.StringLiteral(value.ToString() ?? string.Empty),
    };

    static string RenderPath(string path) => string.Join('.', path.Split('.').Select(Identifiers.ToPascalCase));

    static string RenderTemplate(TemplateExpressionSyntax template, IExpressionContext context)
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
                builder.Append('{').Append(Render(interpolation.Expression, context)).Append('}');
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

    static string EscapeInterpolated(string text) =>
        CSharpCodeBuilder.Escape(text).Replace("{", "{{", StringComparison.Ordinal).Replace("}", "}}", StringComparison.Ordinal);
}
