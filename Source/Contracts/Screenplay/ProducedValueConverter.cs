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
/// Converts a Screenplay <see cref="ExpressionSyntax"/> into the <see cref="ProducedValueKind"/> and expression text
/// the engine evaluates at runtime — the vocabulary shared by a produced event's property mappings, the event source
/// a <c>produces … for</c> names, and the constant side of a condition.
/// </summary>
public static class ProducedValueConverter
{
    /// <summary>
    /// Converts an expression into the kind and expression text describing where its value comes from.
    /// </summary>
    /// <param name="expression">The expression to convert.</param>
    /// <returns>The kind and its expression text.</returns>
    public static (ProducedValueKind Kind, string Expression) Convert(ExpressionSyntax expression) =>
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

    /// <summary>
    /// Converts a property mapping into the Stage record stating how that property gets its value.
    /// </summary>
    /// <param name="mapping">The mapping to convert.</param>
    /// <returns>The Stage produced-event property.</returns>
    /// <remarks>
    /// Shared by every construct that fills a payload from a mapping — a command's <c>produces</c>, a
    /// reaction's <c>produces</c> and <c>invokes</c>, and a capture's <c>append</c> — so the same written
    /// mapping converts the same way whichever of them wrote it.
    /// </remarks>
    public static ProducedEventProperty Property(PropertyMappingSyntax mapping)
    {
        var (kind, expression) = Convert(mapping.Source);

        return new(mapping.Property, kind, expression);
    }

    /// <summary>
    /// Converts an expression used as a tag into its constant text.
    /// </summary>
    /// <param name="expression">The expression to convert.</param>
    /// <returns>The tag text, or an empty string when the tag has no constant form.</returns>
    /// <remarks>
    /// A tag has to be a constant string by the time the event is appended. Literal and bare-identifier tags render
    /// directly; a tag sourced from the runtime context has no constant form and is left off.
    /// </remarks>
    public static string Tag(ExpressionSyntax expression) =>
        expression switch
        {
            LiteralExpressionSyntax { Value: string text } => text,
            PathExpressionSyntax path => path.Path,
            _ => string.Empty
        };

    /// <summary>
    /// Converts a set of tag declarations into the constant tags they carry, dropping those with no constant form.
    /// </summary>
    /// <param name="tags">The tag declarations, or <see langword="null"/> when none are declared.</param>
    /// <returns>The constant tag texts.</returns>
    public static IReadOnlyList<string> Tags(IEnumerable<TagSyntax>? tags) =>
        [.. (tags ?? []).Select(tag => Tag(tag.Value)).Where(tag => tag.Length > 0)];

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
}
