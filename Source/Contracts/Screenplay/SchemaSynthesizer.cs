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
    /// The schema keyword naming the concept a property is typed as.
    /// </summary>
    /// <remarks>
    /// A concept resolves to its underlying primitive in the schema, which erases which concept it was. Naming it
    /// keeps the property joinable back to the concept it came from.
    /// </remarks>
    public const string ConceptKeyword = "x-concept";

    /// <summary>
    /// The schema keyword carrying the attributes declared on the concept a property is typed as, as a map of
    /// attribute name to its declared reason (an empty string when none was declared).
    /// </summary>
    /// <remarks>
    /// Carries <c>@pii</c> and <c>@sensitive</c> — and anything the language adds later, since the map is keyed by
    /// whatever the concept declares rather than by a fixed set. Without it a compliance marker does not survive
    /// the import at all: the property is indistinguishable from an ordinary string once the concept is resolved.
    /// <para>
    /// This states the marker; it does not enforce it. Chronicle carries its own <c>compliance</c> schema keyword
    /// that drives encryption at rest, which is a separate and deliberate step.
    /// </para>
    /// </remarks>
    public const string ConceptAttributesKeyword = "x-conceptAttributes";

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

    // Applied after the underlying type is resolved so the marker lands on the node that actually holds the value —
    // for a collection that is the item schema, which is where a reader of the property looks.
    static JsonNode Annotate(JsonNode node, ConceptSyntax concept)
    {
        if (node is not JsonObject schema)
        {
            return node;
        }

        schema[ConceptKeyword] = concept.Name;

        var attributes = concept.Attributes.ToArray();
        if (attributes.Length == 0)
        {
            return schema;
        }

        // A concept based on another concept resolves through this method twice; the inner attributes are already
        // on the node, so they are merged into rather than replaced.
        if (schema[ConceptAttributesKeyword] is not JsonObject declared)
        {
            declared = [];
            schema[ConceptAttributesKeyword] = declared;
        }

        foreach (var attribute in attributes)
        {
            declared[attribute.Name] = attribute.Reason ?? string.Empty;
        }

        return schema;
    }

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
            var node = concept.IsEnum
                ? new JsonObject { ["type"] = "string", ["enum"] = new JsonArray([.. concept.Values.Select(value => (JsonNode)JsonValue.Create(value))]) }
                : ForTypeName(concept.Type);

            return Annotate(node, concept);
        }

        return new JsonObject { ["type"] = "object" };
    }
}
