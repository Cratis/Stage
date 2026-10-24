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
    public static string For(SemanticApplication application, IEnumerable<SemanticProperty> properties) => Create(application, properties, new CommandReferences(application, null, string.Empty, SourceLocation.Start));

    /// <summary>
    /// Creates a command schema while reporting unsupported semantic types instead of throwing.
    /// </summary>
    /// <param name="application">The semantic application.</param>
    /// <param name="properties">The command properties.</param>
    /// <param name="diagnostics">The collector, or null for legacy throwing behavior.</param>
    /// <param name="path">The owning command path.</param>
    /// <param name="location">The source location supplied by the caller.</param>
    /// <returns>The complete schema, or null if any type cannot be translated.</returns>
    public static string? For(SemanticApplication application, IEnumerable<SemanticProperty> properties, ConversionDiagnostics? diagnostics, string path, SourceLocation location)
    {
        if (diagnostics is null) return For(application, properties);
        var all = properties.Where(property => !property.IsGenerated).ToArray();
        var omissions = new List<(string Code, string Construct, string Path)>();
        foreach (var concept in application.Concepts.Where(concept => concept.Values.IsEmpty && !Supported(concept.Primitive)))
        {
            omissions.Add(("STAGE-SCENE-SCHEMA-002", $"concept primitive {concept.Primitive}", $"{path}.{concept.Name}"));
        }
        foreach (var property in all)
        {
            if (property.Type.Kind is not (SemanticTypeReferenceKind.Primitive or SemanticTypeReferenceKind.Concept or SemanticTypeReferenceKind.CompositeType))
            {
                omissions.Add(("STAGE-SCENE-SCHEMA-001", $"command property type {property.Type.Kind}", $"{path}.{property.Name}"));
            }
            else if (property.Type.Kind == SemanticTypeReferenceKind.Primitive && !Supported(property.Type.Primitive))
            {
                omissions.Add(("STAGE-SCENE-SCHEMA-002", $"command property primitive {property.Type.Primitive}", $"{path}.{property.Name}"));
            }
        }
        foreach (var omission in omissions)
        {
            diagnostics.Report(new(omission.Code, DiagnosticSeverity.Error, omission.Construct, omission.Path, location, $"Command schema '{path}' was omitted because of unsupported {omission.Construct}."));
        }

        return omissions.Count == 0 ? Create(application, all, new CommandReferences(application, diagnostics, path, location)) : null;
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

    static string Create(SemanticApplication application, IEnumerable<SemanticProperty> properties, CommandReferences references)
    {
        var concepts = application.Concepts.ToDictionary(
            concept => concept.Name,
            concept => new ConceptSyntax(
                concept.Name,
                concept.Values.IsEmpty ? PrimitiveName(concept.Primitive) : "Enum",
                [],
                concept.Values,
                SourceLocation.Start),
            StringComparer.Ordinal);
        var schemas = new SchemaSynthesizer(concepts, CompositeTypes(application));

        return schemas.ForCommandProperties(
            properties.Where(property => !property.IsGenerated).Select(property => new PropertySyntax(
                property.Name,
                new TypeRefSyntax(TypeName(application, property.Type), property.Type.IsCollection, property.Type.IsOptional, SourceLocation.Start),
                SourceLocation.Start,
                property.IsIdentifier)),
            references);
    }

    // The members of every composite type, so a record property is presented as the fields it holds rather than as an opaque object. A member
    // whose type no form can describe is left an open object, which the form then reports for that member instead of dropping the record.
    static Dictionary<string, IReadOnlyList<PropertySyntax>> CompositeTypes(SemanticApplication application) =>
        application.Types.ToDictionary(
            type => type.Name,
            type => (IReadOnlyList<PropertySyntax>)[.. type.Properties.Select(member => new PropertySyntax(
                member.Name,
                new TypeRefSyntax(MemberTypeName(application, member.Type), member.Type.IsCollection, member.Type.IsOptional, SourceLocation.Start),
                SourceLocation.Start,
                member.IsIdentifier))],
            StringComparer.Ordinal);

    static string MemberTypeName(SemanticApplication application, SemanticTypeReference reference)
    {
        try
        {
            return TypeName(application, reference);
        }
        catch (UnsupportedCommandFormSchema)
        {
            return string.Empty;
        }
    }

    static bool Supported(SemanticPrimitiveType primitive) => primitive is SemanticPrimitiveType.Uuid or SemanticPrimitiveType.Text or SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber or SemanticPrimitiveType.Boolean or SemanticPrimitiveType.Date or SemanticPrimitiveType.DateTime;

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
