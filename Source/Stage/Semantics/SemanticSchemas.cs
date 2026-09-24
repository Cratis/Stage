// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Builds Chronicle schemas from the typed semantic contracts.
/// </summary>
public static class SemanticSchemas
{
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
        if (reference.IsOptional)
        {
            return new JsonObject
            {
                ["anyOf"] = new JsonArray(
                    TypeSchema(reference with { IsOptional = false }, application),
                    new JsonObject { ["type"] = "null" })
            };
        }

        if (reference.IsCollection)
        {
            return new JsonObject { ["type"] = "array", ["items"] = TypeSchema(reference with { IsCollection = false }, application) };
        }

        if (reference.Kind == SemanticTypeReferenceKind.CompositeType)
        {
            var type = application.Types.Single(candidate => candidate.Id == reference.Target);
            return JsonNode.Parse(Schema(type.Properties, application))!.AsObject();
        }

        var primitive = reference.Kind == SemanticTypeReferenceKind.Concept
            ? application.Concepts.Single(concept => concept.Id == reference.Target).Primitive
            : reference.Primitive;
        var (typeName, format) = primitive switch
        {
            SemanticPrimitiveType.Uuid => ("string", "uuid"),
            SemanticPrimitiveType.Text => ("string", null),
            SemanticPrimitiveType.WholeNumber => ("integer", null),
            SemanticPrimitiveType.DecimalNumber => ("number", null),
            SemanticPrimitiveType.Boolean => ("boolean", null),
            SemanticPrimitiveType.Date => ("string", "date"),
            SemanticPrimitiveType.DateTime => ("string", "date-time"),
            _ => throw new UnsupportedSemanticSchema()
        };
        var schema = new JsonObject { ["type"] = typeName };
        if (format is not null)
        {
            schema["format"] = format;
        }

        return schema;
    }
}

/// <summary>
/// The exception that is thrown when a semantic property cannot be registered as a Chronicle schema.
/// </summary>
public sealed class UnsupportedSemanticSchema() : Exception("A semantic property cannot be registered as a Chronicle schema.");
