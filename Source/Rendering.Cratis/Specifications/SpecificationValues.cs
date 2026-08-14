// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Syntax;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Types;

namespace Cratis.Stage.Rendering.Cratis.Specifications;

/// <summary>
/// Renders the values a specification states, as the C# the declared type can take.
/// </summary>
/// <remarks>
/// A Screenplay document writes every value as a literal — a uuid is written as a string, an enum member as a
/// string — because the language's types say what they mean. The generated C# types do not accept that
/// literal directly, so each one is rendered through the conversion its declared type actually has. A literal
/// no conversion reaches is reported rather than forced.
/// </remarks>
public static class SpecificationValues
{
    /// <summary>
    /// Renders the constructor argument for one declared property, from the values the specification states.
    /// </summary>
    /// <param name="property">The declared property to render an argument for.</param>
    /// <param name="values">The values the specification states.</param>
    /// <param name="owner">What declares the property, for diagnostics.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> to resolve the type against.</param>
    /// <param name="diagnostics">Collects anything that could not be rendered faithfully.</param>
    /// <returns>The rendered argument.</returns>
    public static string For(
        PropertySyntax property,
        IEnumerable<PropertyMappingSyntax> values,
        string owner,
        ApplicationSet applicationSet,
        ICollection<string> diagnostics)
    {
        var stated = values.FirstOrDefault(value => string.Equals(value.Property, property.Name, StringComparison.OrdinalIgnoreCase));
        if (stated is null)
        {
            return "default!";
        }

        if (stated.Source is not LiteralExpressionSyntax literal)
        {
            diagnostics.Add(
                $"'{property.Name}' of '{owner}' is stated as a {stated.Source.GetType().Name}, which a specification " +
                "value cannot be — only a literal is rendered.");
            return "default!";
        }

        return Literal(literal.Value, property.Type, property.Name, owner, applicationSet, diagnostics);
    }

    /// <summary>
    /// Renders a literal as the declared type takes it.
    /// </summary>
    /// <param name="value">The literal value the document states.</param>
    /// <param name="declared">The declared type of what it fills.</param>
    /// <param name="property">The property being filled, for diagnostics.</param>
    /// <param name="owner">What declares the property, for diagnostics.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> to resolve the underlying type against.</param>
    /// <param name="diagnostics">Collects anything that could not be rendered faithfully.</param>
    /// <returns>The rendered literal.</returns>
    public static string Literal(
        object? value,
        TypeRefSyntax declared,
        string property,
        string owner,
        ApplicationSet applicationSet,
        ICollection<string> diagnostics)
    {
        var type = TypeResolver.Resolve(declared, applicationSet);
        if (type.IsCollection || type.Kind == ResolvedTypeKind.Composite)
        {
            diagnostics.Add($"'{property}' of '{owner}' is a {(type.IsCollection ? "collection" : "composite type")}, which a stated literal cannot fill.");
            return "default!";
        }

        if (type.Kind == ResolvedTypeKind.Enum && value is string member)
        {
            return $"{type.ClrTypeName}.{Identifiers.ToPascalCase(member)}";
        }

        var underlying = Underlying(declared, type, applicationSet);
        var rendered = underlying switch
        {
            "string" when value is string text => CSharpCodeBuilder.StringLiteral(text),
            "Guid" when value is string text => $"Guid.Parse({CSharpCodeBuilder.StringLiteral(text)})",
            "DateOnly" when value is string text => $"DateOnly.Parse({CSharpCodeBuilder.StringLiteral(text)}, CultureInfo.InvariantCulture)",
            "DateTimeOffset" when value is string text => $"DateTimeOffset.Parse({CSharpCodeBuilder.StringLiteral(text)}, CultureInfo.InvariantCulture)",
            "bool" when value is bool boolean => boolean ? "true" : "false",
            "int" when value is int or long or double or decimal && Integral(value) is { } integer => integer.ToString(CultureInfo.InvariantCulture),
            "decimal" when value is int or long or double or decimal => $"{Convert.ToDecimal(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)}m",
            _ => null,
        };

        if (rendered is null)
        {
            diagnostics.Add(
                $"'{property}' of '{owner}' is stated as {Describe(value)}, which '{type.ClrTypeName}' cannot take.");
            return "default!";
        }

        return rendered;
    }

    /// <summary>
    /// Whether rendering a literal into the given type needs <c>System.Globalization</c> in scope.
    /// </summary>
    /// <param name="rendered">The rendered literal.</param>
    /// <returns>True when the rendering parses a culture-sensitive value.</returns>
    public static bool NeedsGlobalization(string rendered) => rendered.Contains("CultureInfo.InvariantCulture", StringComparison.Ordinal);

    /// <summary>
    /// The value as an <see langword="int"/>, or <see langword="null"/> when it is not one. A stated 1.5 is not
    /// the integer 2, and a value outside the range is not an integer at all — rounding either would put a value
    /// in the rendered output that the document never stated.
    /// </summary>
    /// <param name="value">The stated literal.</param>
    /// <returns>The integer, or <see langword="null"/>.</returns>
    static int? Integral(object value)
    {
        var number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        return number == decimal.Truncate(number) && number >= int.MinValue && number <= int.MaxValue
            ? (int)number
            : null;
    }

    static string? Underlying(TypeRefSyntax declared, ResolvedType type, ApplicationSet applicationSet)
    {
        if (type.Kind == ResolvedTypeKind.Primitive)
        {
            return type.ClrTypeName;
        }

        if (type.Kind == ResolvedTypeKind.Concept && applicationSet.Concepts.TryGetValue(declared.Name, out var concept))
        {
            return TypeResolver.Resolve(new TypeRefSyntax(concept.Type, false, false, concept.Location), applicationSet) is
                { Kind: ResolvedTypeKind.Primitive } resolved ? resolved.ClrTypeName : null;
        }

        return null;
    }

    static string Describe(object? value) => value switch
    {
        null => "nothing",
        string text => $"the text '{text}'",
        bool boolean => boolean ? "true" : "false",
        _ => $"the value '{value}'",
    };
}
