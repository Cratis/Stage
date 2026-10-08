// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Stage.Rendering.Cratis.Types;

namespace Cratis.Stage.Rendering.Cratis.Expressions;

/// <summary>
/// Admits and renders destinations whose scalar type and command-property lineage are known.
/// </summary>
internal static class CommandEventSourceExpression
{
    /// <summary>
    /// Renders a destination with the same conversions in handlers and specifications.
    /// </summary>
    /// <param name="destination">The authored destination.</param>
    /// <param name="command">The source command.</param>
    /// <param name="application">The surrounding types.</param>
    /// <param name="propertyValue">Renders a command property in the caller's scope.</param>
    /// <returns>The event-source expression.</returns>
    public static string Render(ExpressionSyntax destination, CommandSyntax command, ApplicationSet application, Func<PropertySyntax, string> propertyValue)
    {
        var property = Property(destination, command, application);
        var type = property is null ? null : TypeResolver.Resolve(property.Type, application);
        var implicitConversion = type is not null &&
            ((type.Kind == ResolvedTypeKind.Primitive && (string.Equals(type.ClrTypeName, "string", StringComparison.Ordinal) || string.Equals(type.ClrTypeName, "Guid", StringComparison.Ordinal))) ||
             (type.Kind == ResolvedTypeKind.Concept && application.IdentifierConceptNames.Contains(property!.Type.Name)));
        var expression = RenderValue(destination, command, application, propertyValue);

        return EventSourceExpression.Render(expression, implicitConversion);
    }

    /// <summary>
    /// Enumerates every property a destination reads, rejecting expressions with unknown lineage.
    /// </summary>
    /// <param name="destination">The authored destination.</param>
    /// <param name="command">The source command.</param>
    /// <param name="application">The surrounding types.</param>
    /// <returns>The referenced scalar properties.</returns>
    /// <exception cref="UnsupportedExpression">The destination has unknown lineage or is not scalar.</exception>
    public static IEnumerable<PropertySyntax> Properties(ExpressionSyntax destination, CommandSyntax command, ApplicationSet application)
    {
        if (Property(destination, command, application) is { } property)
        {
            yield return property;
        }
        else if (destination is TemplateExpressionSyntax template)
        {
            foreach (var referenced in template.Parts.OfType<TemplateInterpolationSyntax>().SelectMany(part => Properties(part.Expression, command, application)))
            {
                yield return referenced;
            }
        }
        else if (destination is not LiteralExpressionSyntax { Value: string or bool or int or long or double or decimal })
        {
            throw new UnsupportedExpression(destination);
        }
    }

    static PropertySyntax? Property(ExpressionSyntax destination, CommandSyntax command, ApplicationSet application)
    {
        var path = destination switch
        {
            PathExpressionSyntax value => value.Path,
            ContextExpressionSyntax value when value.Path.StartsWith("command.", StringComparison.Ordinal) => value.Path[8..],
            _ => null,
        };
        if (path is null)
        {
            return null;
        }

        var property = command.Properties.FirstOrDefault(candidate => string.Equals(candidate.Name, path, StringComparison.OrdinalIgnoreCase))
            ?? throw new UnsupportedExpression(destination);

        var type = TypeResolver.Resolve(property.Type, application);
        if (type.IsCollection || type.IsOptional || type.Kind is ResolvedTypeKind.Composite or ResolvedTypeKind.Unresolved)
        {
            throw new UnsupportedExpression(destination);
        }

        return property;
    }

    static string RenderValue(ExpressionSyntax destination, CommandSyntax command, ApplicationSet application, Func<PropertySyntax, string> propertyValue)
    {
        if (Property(destination, command, application) is { } property)
        {
            return propertyValue(property);
        }

        if (destination is TemplateExpressionSyntax template)
        {
            var parts = template.Parts.Select(part => part is TemplateInterpolationSyntax interpolation
                ? new TemplateInterpolationSyntax(new RawExpressionSyntax(RenderValue(interpolation.Expression, command, application, propertyValue), interpolation.Expression.Location), interpolation.Location)
                : part);
            return ExpressionRenderer.Render(template with { Parts = [.. parts] });
        }

        if (destination is LiteralExpressionSyntax { Value: string or bool or int or long or double or decimal })
        {
            return ExpressionRenderer.Render(destination);
        }

        throw new UnsupportedExpression(destination);
    }
}
