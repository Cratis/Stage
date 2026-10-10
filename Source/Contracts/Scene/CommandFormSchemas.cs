// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Scene.Model.Forms;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Stage.Contracts.Screenplay;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The command metadata a native command form is built from: the presentation schema of a command's properties and
/// the fields that schema implies.
/// </summary>
/// <remarks>
/// One implementation serves the live Stage, which adds it to the Scene it serves, and the Cratis renderer, which
/// adds it to the Scene a generated application runs - so a form shows the same fields, types and required markers
/// in both. The schema keeps the presentation encoding (concept names, enum values and required fields) rather
/// than Chronicle's storage schema encoding.
/// </remarks>
public static class CommandFormSchemas
{
    /// <summary>
    /// Creates the form schema for a command's properties.
    /// </summary>
    /// <param name="application">The semantic application declaring the concepts and types the properties use.</param>
    /// <param name="properties">The command properties.</param>
    /// <returns>The JSON schema text.</returns>
    /// <exception cref="UnsupportedCommandFormSchema">Thrown when a property has a type no form schema can describe.</exception>
    public static string For(SemanticApplication application, IEnumerable<SemanticProperty> properties)
    {
        var schemas = new SchemaSynthesizer(application.Concepts.ToDictionary(
            concept => concept.Name,
            concept => new ConceptSyntax(
                concept.Name,
                concept.Values.IsEmpty ? PrimitiveName(concept.Primitive) : "Enum",
                [],
                concept.Values,
                SourceLocation.Start),
            StringComparer.Ordinal));

        return schemas.ForProperties(properties.Select(property => new PropertySyntax(
            property.Name,
            new TypeRefSyntax(TypeName(application, property.Type), property.Type.IsCollection, property.Type.IsOptional, SourceLocation.Start),
            SourceLocation.Start,
            property.IsIdentifier)));
    }

    /// <summary>
    /// Gets the form fields a command schema implies, one per property, labeled by its words.
    /// </summary>
    /// <param name="schema">The JSON schema text.</param>
    /// <returns>The fields, or none when the schema has no properties or is not JSON.</returns>
    public static IReadOnlyList<FormField> Fields(string schema)
    {
        if (string.IsNullOrWhiteSpace(schema))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(schema);
            if (!document.RootElement.TryGetProperty("properties", out var properties) || properties.ValueKind != JsonValueKind.Object)
            {
                return [];
            }

            return [.. properties.EnumerateObject().Select(property => new FormField(property.Name, null, null, Humanize(property.Name), null))];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    static string PrimitiveName(SemanticPrimitiveType primitive) => primitive switch
    {
        SemanticPrimitiveType.Uuid => "Uuid",
        SemanticPrimitiveType.Text => "String",
        SemanticPrimitiveType.WholeNumber => "Int",
        SemanticPrimitiveType.DecimalNumber => "Decimal",
        SemanticPrimitiveType.Boolean => "Bool",
        SemanticPrimitiveType.Date => "Date",
        SemanticPrimitiveType.DateTime => "DateTime",
        _ => throw new UnsupportedCommandFormSchema(primitive.ToString())
    };

    static string TypeName(SemanticApplication application, SemanticTypeReference reference) => reference.Kind switch
    {
        SemanticTypeReferenceKind.Primitive => PrimitiveName(reference.Primitive),
        SemanticTypeReferenceKind.Concept => application.Concepts.Single(concept => concept.Id == reference.Target).Name,
        SemanticTypeReferenceKind.CompositeType => application.Types.Single(type => type.Id == reference.Target).Name,
        _ => throw new UnsupportedCommandFormSchema(reference.Kind.ToString())
    };

    static string Humanize(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var text = new StringBuilder(value.Length * 2);
        text.Append(char.ToUpperInvariant(value[0]));
        for (var index = 1; index < value.Length; index++)
        {
            var character = value[index];
            if (char.IsUpper(character) && !char.IsUpper(value[index - 1]))
            {
                text.Append(' ');
            }

            text.Append(character);
        }

        return text.ToString();
    }
}
