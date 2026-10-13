// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Builds Chronicle schemas from the typed semantic contracts.
/// </summary>
public static class SemanticSchemas
{
    static readonly FrozenDictionary<SemanticPrimitiveType, (string Type, string? Format)> _primitiveShapes =
        new Dictionary<SemanticPrimitiveType, (string Type, string? Format)>
        {
            [SemanticPrimitiveType.Uuid] = ("string", "uuid"),
            [SemanticPrimitiveType.Text] = ("string", null),
            [SemanticPrimitiveType.WholeNumber] = ("integer", null),
            [SemanticPrimitiveType.DecimalNumber] = ("number", null),
            [SemanticPrimitiveType.Boolean] = ("boolean", null),
            [SemanticPrimitiveType.Date] = ("string", "date"),
            [SemanticPrimitiveType.DateTime] = ("string", "date-time")
        }.ToFrozenDictionary();

    /// <summary>
    /// Builds a JSON object schema for semantic properties.
    /// </summary>
    /// <param name="properties">The declared properties.</param>
    /// <param name="application">The application declaring their types.</param>
    /// <returns>The schema in JSON form.</returns>
    public static string Schema(IEnumerable<SemanticProperty> properties, SemanticApplication application)
    {
        var nodes = new JsonObject();
        var required = new JsonArray();
        foreach (var property in properties)
        {
            nodes[property.Name] = TypeSchema(property.Type, application);
            if (!property.Type.IsOptional)
            {
                required.Add(property.Name);
            }
        }

        return new JsonObject { ["type"] = "object", ["properties"] = nodes, ["required"] = required }.ToJsonString();
    }

    static JsonObject TypeSchema(SemanticTypeReference reference, SemanticApplication application)
    {
        var schema = TypeShape(reference, application);
        if (!reference.IsCollection && reference.Kind == SemanticTypeReferenceKind.Concept)
        {
            var compliance = application.Concepts.Single(concept => concept.Id == reference.Target).Compliance;
            if (compliance?.Personal is not null)
            {
                schema["compliance"] = Metadata("PII");
            }
            else if (compliance?.Secret is { } secret)
            {
                schema["security"] = Metadata(secret.Scope switch
                {
                    "namespace" => "EncryptedNamespace",
                    "global" => "EncryptedGlobal",
                    _ => "EncryptedSubject"
                });
            }
        }

        return schema;
    }

    static JsonArray Metadata(string type) => new(new JsonObject { ["metadataType"] = type, ["details"] = string.Empty });

    static JsonObject TypeShape(SemanticTypeReference reference, SemanticApplication application)
    {
        if (reference.IsOptional) return OptionalShape(reference, application);
        if (reference.IsCollection)
        {
            return new JsonObject { ["type"] = "array", ["items"] = TypeSchema(reference with { IsCollection = false }, application) };
        }

        if (reference.Kind == SemanticTypeReferenceKind.CompositeType)
        {
            var type = application.Types.Single(candidate => candidate.Id == reference.Target);
            return JsonNode.Parse(Schema(type.Properties, application))!.AsObject();
        }

        return PrimitiveShape(reference.Kind == SemanticTypeReferenceKind.Concept
            ? application.Concepts.Single(concept => concept.Id == reference.Target).Primitive
            : reference.Primitive);
    }

    // Keep protection metadata on the primitive node Chronicle inspects, while allowing absent values.
    static JsonObject OptionalShape(SemanticTypeReference reference, SemanticApplication application)
    {
        var required = reference with { IsOptional = false };
        if (IsProtectedConcept(reference, application))
        {
            var schema = TypeShape(required, application);
            schema["type"] = new JsonArray(schema["type"]!.GetValue<string>(), "null");

            return schema;
        }

        return new JsonObject { ["anyOf"] = new JsonArray(TypeShape(required, application), new JsonObject { ["type"] = "null" }) };
    }

    static bool IsProtectedConcept(SemanticTypeReference reference, SemanticApplication application) =>
        !reference.IsCollection && reference.Kind == SemanticTypeReferenceKind.Concept &&
        application.Concepts.Single(concept => concept.Id == reference.Target).Compliance is not null;

    static JsonObject PrimitiveShape(SemanticPrimitiveType primitive)
    {
        if (!_primitiveShapes.TryGetValue(primitive, out var shape)) throw new UnsupportedSemanticSchema();
        var schema = new JsonObject { ["type"] = shape.Type };
        if (shape.Format is not null)
        {
            schema["format"] = shape.Format;
        }

        return schema;
    }
}

/// <summary>
/// The exception that is thrown when a semantic property cannot be registered as a Chronicle schema.
/// </summary>
public sealed class UnsupportedSemanticSchema() : Exception("A semantic property cannot be registered as a Chronicle schema.");
