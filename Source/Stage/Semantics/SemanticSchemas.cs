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
    const string DecimalFormat = "decimal";
    static readonly string[] _branchKeys = ["anyOf", "oneOf"];

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

    /// <summary>
    /// Gets the schema of one type reference, the way a property of an event or read model is registered.
    /// </summary>
    /// <param name="reference">The type reference.</param>
    /// <param name="application">The application that declares the concepts and types the reference names.</param>
    /// <returns>The JSON schema node.</returns>
    public static JsonObject TypeSchemaOf(SemanticTypeReference reference, SemanticApplication application) => TypeSchema(reference, application);

    /// <summary>
    /// Rewrites a built event schema so its number nodes carry Chronicle's exact decimal format, or the format a stored schema already carries.
    /// </summary>
    /// <param name="built">The schema <see cref="Schema"/> built; it is not changed.</param>
    /// <param name="stored">
    /// The schema already stored for the event type, or <see langword="null"/> when the type is not registered yet. Without a stored schema every number node
    /// is made exact. With one, each number node copies the format of the stored node at the same path and never gains or loses one, because the kernel
    /// refuses a registration that changes the format of an existing property.
    /// </param>
    /// <returns>The rewritten schema.</returns>
    public static JsonObject WithDecimalFormats(JsonObject built, JsonObject? stored)
    {
        var rewritten = (JsonObject)built.DeepClone();
        ApplyDecimalFormats(rewritten, stored, stored is null);
        return rewritten;
    }

    /// <summary>
    /// Gets whether every number node of a stored schema carries Chronicle's exact decimal format, which is what lets a stored decimal be read back exactly.
    /// </summary>
    /// <param name="stored">The stored schema.</param>
    /// <returns><see langword="true"/> when no number node of the schema lacks the exact format.</returns>
    public static bool HasOnlyExactNumbers(JsonObject stored) => AllNumbers(stored, IsExactFormat);

    static void ApplyDecimalFormats(JsonNode? node, JsonNode? counterpart, bool exact)
    {
        if (node is not JsonObject schema) return;
        if (IsNumber(schema))
        {
            if (exact)
            {
                schema["format"] = DecimalFormat;
            }
            else if (NumberNodeOf(counterpart)?["format"] is JsonValue format && format.TryGetValue<string>(out var stored))
            {
                schema["format"] = stored;
            }
        }

        if (schema["properties"] is JsonObject properties)
        {
            foreach (var (name, child) in properties.ToArray())
            {
                ApplyDecimalFormats(child, (counterpart as JsonObject)?["properties"]?[name], exact);
            }
        }

        ApplyDecimalFormats(schema["items"], (counterpart as JsonObject)?["items"], exact);
        foreach (var key in _branchKeys)
        {
            if (schema[key] is not JsonArray branches) continue;
            for (var index = 0; index < branches.Count; index++)
            {
                ApplyDecimalFormats(branches[index], BranchOf(counterpart, key, index), exact);
            }
        }
    }

    // A stored optional number may be a plain number node or an anyOf, whichever shape registered it; both name the same property.
    static JsonNode? BranchOf(JsonNode? counterpart, string key, int index)
    {
        if (counterpart is not JsonObject stored) return null;
        if (stored[key] is not JsonArray branches) return stored;

        return index < branches.Count ? branches[index] : null;
    }

    static JsonObject? NumberNodeOf(JsonNode? node)
    {
        if (node is not JsonObject schema) return null;
        if (IsNumber(schema)) return schema;

        return _branchKeys
            .Select(key => schema[key] as JsonArray)
            .Where(branches => branches is not null)
            .SelectMany(branches => branches!.OfType<JsonObject>())
            .FirstOrDefault(IsNumber);
    }

    static bool IsNumber(JsonObject schema) => schema["type"] switch
    {
        JsonValue value when value.TryGetValue<string>(out var type) => type == "number",
        JsonArray types => types.Any(type => type is JsonValue value && value.TryGetValue<string>(out var name) && name == "number"),
        _ => false
    };

    static bool IsExactFormat(JsonObject number) =>
        number["format"] is JsonValue format && format.TryGetValue<string>(out var text) && text.TrimEnd('?') == DecimalFormat;

    static bool AllNumbers(JsonNode? node, Func<JsonObject, bool> predicate) => node switch
    {
        JsonObject schema => (!IsNumber(schema) || predicate(schema)) && schema.All(entry => AllNumbers(entry.Value, predicate)),
        JsonArray items => items.All(item => AllNumbers(item, predicate)),
        _ => true
    };

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

    static JsonArray Metadata(string type) => [new JsonObject { ["metadataType"] = type, ["details"] = string.Empty }];

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
