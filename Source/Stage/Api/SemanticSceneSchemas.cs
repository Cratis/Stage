// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Stage.Contracts.Screenplay;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Api;

// Form schemas retain the existing presentation encoding (concept names, enum values and required fields),
// rather than Chronicle's storage schema encoding. Compliance attributes are refused by the semantic compiler.
internal sealed class SemanticSceneSchemas(SemanticApplication application)
{
    readonly SchemaSynthesizer _schemas = new(application.Concepts.ToDictionary(
        concept => concept.Name,
        concept => new ConceptSyntax(
            concept.Name,
            concept.Values.IsEmpty ? PrimitiveName(concept.Primitive) : "Enum",
            [],
            concept.Values,
            SourceLocation.Start),
        StringComparer.Ordinal));

    internal string ForProperties(IEnumerable<SemanticProperty> properties) => _schemas.ForProperties(properties.Select(property => new PropertySyntax(
        property.Name,
        new TypeRefSyntax(TypeName(property.Type), property.Type.IsCollection, property.Type.IsOptional, SourceLocation.Start),
        SourceLocation.Start,
        property.IsIdentifier)));

    static string PrimitiveName(SemanticPrimitiveType primitive) => primitive switch
    {
        SemanticPrimitiveType.Uuid => "Uuid",
        SemanticPrimitiveType.Text => "String",
        SemanticPrimitiveType.WholeNumber => "Int",
        SemanticPrimitiveType.DecimalNumber => "Decimal",
        SemanticPrimitiveType.Boolean => "Bool",
        SemanticPrimitiveType.Date => "Date",
        SemanticPrimitiveType.DateTime => "DateTime",
        _ => throw new UnsupportedSemanticSchema()
    };

    string TypeName(SemanticTypeReference reference) => reference.Kind switch
    {
        SemanticTypeReferenceKind.Primitive => PrimitiveName(reference.Primitive),
        SemanticTypeReferenceKind.Concept => application.Concepts.Single(concept => concept.Id == reference.Target).Name,
        SemanticTypeReferenceKind.CompositeType => application.Types.Single(type => type.Id == reference.Target).Name,
        _ => throw new UnsupportedSemanticSchema()
    };
}
