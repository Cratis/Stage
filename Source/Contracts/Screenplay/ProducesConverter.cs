// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Stage.Contracts.Commands;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Converts the Screenplay <c>produces</c> declarations of a command into the Stage <see cref="ProducedEvent"/> records
/// the engine evaluates at runtime to build and append event payloads.
/// </summary>
public static class ProducesConverter
{
    /// <summary>
    /// Converts a command's produces declarations into their Stage records.
    /// </summary>
    /// <param name="produces">The produces declarations.</param>
    /// <returns>The Stage produced-event definitions, in declaration order.</returns>
    public static IReadOnlyList<ProducedEvent> Convert(IEnumerable<ProducesSyntax> produces) =>
    [
        .. produces.Select(declaration => new ProducedEvent(
            declaration.Event,
            Condition(declaration.When),
            [.. declaration.Mappings.Select(Property)],
            [.. (declaration.Tags ?? []).Select(tag => Tag(tag.Value)).Where(tag => tag.Length > 0)]))
    ];

    // A tag has to be a constant string by the time the event is appended. Literal and bare-identifier tags render
    // directly; a tag sourced from the runtime context has no constant form and is left off.
    static string Tag(ExpressionSyntax expression) =>
        expression switch
        {
            LiteralExpressionSyntax { Value: string text } => text,
            PathExpressionSyntax path => path.Path,
            _ => string.Empty
        };

    static ProducedEventProperty Property(PropertyMappingSyntax mapping)
    {
        var (kind, expression) = Source(mapping.Source);

        return new(mapping.Property, kind, expression);
    }

    static (ProducedValueKind Kind, string Expression) Source(ExpressionSyntax expression) =>
        expression switch
        {
            PathExpressionSyntax path => (ProducedValueKind.CommandProperty, path.Path),
            LiteralExpressionSyntax literal => (ProducedValueKind.Literal, Literal(literal.Value)),
            EventContextExpressionSyntax eventContext => EventContext(eventContext.Path),

            // $context.occurred is the time the event happened; anything else under $context that names the
            // identity resolves against the identity that caused the command.
            ContextExpressionSyntax context => Context(context.Path),
            CausedByExpressionSyntax causedBy => (ProducedValueKind.Identity, causedBy.Property ?? "id"),
            EnvironmentExpressionSyntax environment => (ProducedValueKind.Environment, environment.Name),
            TemplateExpressionSyntax template => (ProducedValueKind.Template, Template(template)),
            _ => (ProducedValueKind.Unsupported, string.Empty)
        };

    static (ProducedValueKind Kind, string Expression) Context(string path)
    {
        if (path.Equals("occurred", StringComparison.OrdinalIgnoreCase))
        {
            return (ProducedValueKind.Occurred, string.Empty);
        }

        return path.StartsWith("identity.", StringComparison.OrdinalIgnoreCase)
            ? (ProducedValueKind.Identity, path["identity.".Length..])
            : (ProducedValueKind.Unsupported, path);
    }

    static (ProducedValueKind Kind, string Expression) EventContext(string path) =>
        path.Equals("occurred", StringComparison.OrdinalIgnoreCase)
            ? (ProducedValueKind.Occurred, string.Empty)
            : (ProducedValueKind.Unsupported, path);

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
                case TemplateInterpolationSyntax interpolation when interpolation.Expression is PathExpressionSyntax path:
                    builder.Append("${").Append(path.Path).Append('}');
                    break;
            }
        }

        return builder.ToString();
    }

    static string Literal(object? value) =>
        value switch
        {
            null => "null",
            string text => JsonSerializer.Serialize(text),
            bool boolean => boolean ? "true" : "false",
            double number => number.ToString(CultureInfo.InvariantCulture),
            _ => JsonSerializer.Serialize(System.Convert.ToString(value, CultureInfo.InvariantCulture))
        };

    static ProducedEventCondition? Condition(ConditionSyntax? condition) =>
        condition switch
        {
            null => null,
            ComparisonConditionSyntax comparison => new ProducedEventComparison(
                comparison.Left,
                Operator(comparison.Operator),
                Source(comparison.Right) is { Kind: ProducedValueKind.Literal } literal ? literal.Expression : "null"),
            LogicalConditionSyntax logical when Condition(logical.Left) is { } left && Condition(logical.Right) is { } right =>
                new ProducedEventLogicalCondition(left, Operator(logical.Operator), right),
            _ => null
        };

    static ProducedEventComparisonOperator Operator(ComparisonOperator @operator) =>
        @operator switch
        {
            ComparisonOperator.Equal => ProducedEventComparisonOperator.Equal,
            ComparisonOperator.NotEqual => ProducedEventComparisonOperator.NotEqual,
            ComparisonOperator.GreaterThan => ProducedEventComparisonOperator.GreaterThan,
            ComparisonOperator.GreaterThanOrEqual => ProducedEventComparisonOperator.GreaterThanOrEqual,
            ComparisonOperator.LessThan => ProducedEventComparisonOperator.LessThan,
            ComparisonOperator.LessThanOrEqual => ProducedEventComparisonOperator.LessThanOrEqual,
            _ => ProducedEventComparisonOperator.Equal
        };

    static ProducedEventLogicalOperator Operator(LogicalOperator @operator) =>
        @operator == LogicalOperator.Or ? ProducedEventLogicalOperator.Or : ProducedEventLogicalOperator.And;
}
