// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Synthesizes JSON Schema strings for commands, events and read models from Screenplay typed properties. Screenplay
/// primitives map to JSON Schema types; concepts resolve to their underlying primitive (or an enumeration); any other
/// type name is treated as an open object reference. The shape matches the <c>{"type":"object","properties":{...}}</c>
/// convention Stage's engine already consumes.
/// </summary>
/// <param name="concepts">The concepts declared in the application, keyed by name, used to resolve concept-typed properties.</param>
public sealed class SchemaSynthesizer(IReadOnlyDictionary<string, ConceptSyntax> concepts)
{
    /// <summary>
    /// The empty object schema used for a command's state schema (Screenplay has no state schema concept).
    /// </summary>
    public const string EmptyObjectSchema = """{"type":"object","properties":{}}""";

    /// <summary>
    /// Synthesizes a JSON Schema object for a set of typed properties (a command or event payload).
    /// </summary>
    /// <param name="properties">The typed properties.</param>
    /// <returns>The JSON Schema string.</returns>
    public string ForProperties(IEnumerable<PropertySyntax> properties)
    {
        var propertyList = properties.ToArray();
        var schemaProperties = new JsonObject();
        var required = new JsonArray();

        foreach (var property in propertyList)
        {
            schemaProperties[property.Name] = ForType(property.Type);
            if (!property.Type.IsOptional)
            {
                required.Add(property.Name);
            }
        }

        var schema = new JsonObject { ["type"] = "object", ["properties"] = schemaProperties };
        if (required.Count > 0)
        {
            schema["required"] = required;
        }

        return schema.ToJsonString();
    }

    /// <summary>
    /// Synthesizes a JSON Schema object for a read model from its property names and optional type-name hints.
    /// Screenplay does not declare read-model property types, so a <see langword="null"/> hint produces an open schema.
    /// </summary>
    /// <param name="properties">The ordered read-model properties, each paired with a type-name hint or <see langword="null"/> for open.</param>
    /// <returns>The JSON Schema string.</returns>
    public string ForReadModel(IEnumerable<KeyValuePair<string, string?>> properties)
    {
        var schemaProperties = new JsonObject();
        foreach (var (name, hint) in properties)
        {
            schemaProperties[name] = hint is null ? new JsonObject() : ForTypeName(hint);
        }

        return new JsonObject { ["type"] = "object", ["properties"] = schemaProperties }.ToJsonString();
    }

    static JsonObject? Primitive(string name) =>
        name switch
        {
            "Uuid" => new JsonObject { ["type"] = "string", ["format"] = "uuid" },
            "String" => new JsonObject { ["type"] = "string" },
            "Int" => new JsonObject { ["type"] = "integer" },
            "Decimal" => new JsonObject { ["type"] = "number" },
            "Bool" => new JsonObject { ["type"] = "boolean" },
            "Date" => new JsonObject { ["type"] = "string", ["format"] = "date" },
            "DateTime" => new JsonObject { ["type"] = "string", ["format"] = "date-time" },
            _ => null
        };

    JsonNode ForType(TypeRefSyntax type)
    {
        var inner = ForTypeName(type.Name);

        return type.IsCollection ? new JsonObject { ["type"] = "array", ["items"] = inner } : inner;
    }

    JsonNode ForTypeName(string name)
    {
        var primitive = Primitive(name);
        if (primitive is not null)
        {
            return primitive;
        }

        if (concepts.TryGetValue(name, out var concept))
        {
            return concept.IsEnum
                ? new JsonObject { ["type"] = "string", ["enum"] = new JsonArray([.. concept.Values.Select(value => (JsonNode)JsonValue.Create(value))]) }
                : ForTypeName(concept.Type);
        }

        return new JsonObject { ["type"] = "object" };
    }
}
