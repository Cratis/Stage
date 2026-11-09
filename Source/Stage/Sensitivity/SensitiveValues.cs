// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Screenplay;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Sensitivity;

/// <summary>
/// Keeps values typed as a secret concept out of everything a command sends back to the caller: the payload echoed as
/// the response, a modeled response, and the messages raised when a rule on the payload does not hold. The type graph
/// is read into a <see cref="SensitivityNode"/> tree - from the semantic model or from the command's JSON schema - and
/// every one of those uses walks that same tree.
/// </summary>
internal static class SensitiveValues
{
    // A concept declared with the sensitive marker is the secret of the schema form, whatever else it is marked with.
    const string SecretMarker = "sensitive";

    static readonly HashSet<string> _opaqueSchemaKeywords = new(["anyOf", "oneOf", "allOf", "$ref"], StringComparer.Ordinal);

    /// <summary>
    /// Omits every secret, at any depth, from a payload echoed back, reading the graph from the semantic model.
    /// </summary>
    /// <param name="payload">The payload as the caller sent it.</param>
    /// <param name="properties">The command's declared properties.</param>
    /// <param name="application">The application declaring the concepts and composite types.</param>
    /// <returns>A copy containing only declared members known not to hold secrets.</returns>
    internal static IDictionary<string, JsonElement> Payload(IDictionary<string, JsonElement> payload, IEnumerable<SemanticProperty> properties, SemanticApplication application) =>
        SensitivityNode.Composite(properties.Select(property => (property.Name, FromType(property.Type, application, [])))).FilterPayload(payload);

    /// <summary>
    /// Omits every secret, at any depth, from a payload echoed back, reading the graph from the command's JSON schema.
    /// </summary>
    /// <param name="payload">The payload as the caller sent it.</param>
    /// <param name="schema">The command's JSON schema.</param>
    /// <returns>A copy containing only declared members known not to hold secrets.</returns>
    internal static IDictionary<string, JsonElement> Payload(IDictionary<string, JsonElement> payload, string schema) =>
        FromSchema(schema).FilterPayload(payload);

    /// <summary>
    /// Omits every secret from a modeled command response.
    /// </summary>
    /// <param name="response">The response the command returns.</param>
    /// <param name="declared">The response the command declares.</param>
    /// <param name="application">The application declaring the concepts and composite types.</param>
    /// <returns>The response itself when nothing in it can be secret; otherwise a copy without the secret values.</returns>
    internal static object? Response(object? response, SemanticCommandResponse? declared, SemanticApplication application)
    {
        var node = declared switch
        {
            SemanticScalarCommandResponse scalar => FromType(scalar.Type, application, []),
            SemanticRecordCommandResponse record => SensitivityNode.Composite(record.Fields.Select(field => (ResponseKey(field.Name), FromType(field.Type, application, [])))),
            _ => SensitivityNode.Leaf
        };

        return node.Carries && response is not null
            ? node.Filter(JsonSerializer.SerializeToElement(response))
            : response;
    }

    /// <summary>
    /// Gets whether the value at a (possibly dotted) property path of a command is a secret, so that nothing derived
    /// from it may reach a validation message.
    /// </summary>
    /// <param name="schema">The command's JSON schema.</param>
    /// <param name="path">The property path.</param>
    /// <returns>True when the position is, or is reached through, a secret.</returns>
    internal static bool IsSecretPath(string schema, string path) => FromSchema(schema).IsSecretPath(path);

    /// <summary>
    /// Gets the key a record response field is serialized under.
    /// </summary>
    /// <param name="name">The authored field name.</param>
    /// <returns>The response key.</returns>
    internal static string ResponseKey(string name) => JsonNamingPolicy.CamelCase.ConvertName(GeneratedPascalCase.From(name));

    static SensitivityNode FromType(SemanticTypeReference type, SemanticApplication application, HashSet<SemanticId> visiting)
    {
        if (type.IsCollection)
        {
            return SensitivityNode.Collection(FromType(type with { IsCollection = false }, application, visiting));
        }

        switch (type.Kind)
        {
            case SemanticTypeReferenceKind.Concept:
                return application.Concepts.Single(concept => concept.Id == type.Target).Compliance?.Secret is not null
                    ? SensitivityNode.Secret
                    : SensitivityNode.Leaf;
            case SemanticTypeReferenceKind.CompositeType:
                // A type met again inside itself is cut short, exactly as the schema form cuts it.
                if (!visiting.Add(type.Target))
                {
                    return SensitivityNode.Opaque;
                }

                var node = SensitivityNode.Composite(application.Types.Single(candidate => candidate.Id == type.Target).Properties
                    .Select(property => (property.Name, FromType(property.Type, application, visiting))));
                visiting.Remove(type.Target);
                return node;
            default:
                return SensitivityNode.Leaf;
        }
    }

    static SensitivityNode FromSchema(string schema)
    {
        if (string.IsNullOrWhiteSpace(schema))
        {
            return SensitivityNode.Composite([]);
        }

        try
        {
            using var document = JsonDocument.Parse(schema);
            return FromSchemaNode(document.RootElement);
        }
        catch (JsonException)
        {
            return SensitivityNode.Unreadable;
        }
    }

    static SensitivityNode FromSchemaNode(JsonElement node)
    {
        if (node.ValueKind != JsonValueKind.Object)
        {
            return SensitivityNode.Opaque;
        }

        if (node.TryGetProperty(SchemaSynthesizer.ConceptAttributesKeyword, out var attributes) && attributes.ValueKind == JsonValueKind.Object &&
            attributes.EnumerateObject().Any(attribute => string.Equals(attribute.Name, SecretMarker, StringComparison.OrdinalIgnoreCase)))
        {
            return SensitivityNode.Secret;
        }

        return IsShapeless(node) ? SensitivityNode.Opaque : FromSchemaShape(node);
    }

    static bool IsShapeless(JsonElement node)
    {
        if (!node.TryGetProperty("type", out var type))
        {
            return true;
        }

        if (type.ValueKind != JsonValueKind.String)
        {
            return true;
        }

        return node.EnumerateObject().Any(property => _opaqueSchemaKeywords.Contains(property.Name));
    }

    static SensitivityNode FromSchemaShape(JsonElement node) =>
        node.GetProperty("type").GetString() switch
        {
            "object" when node.TryGetProperty("properties", out var properties) && properties.ValueKind == JsonValueKind.Object =>
                SensitivityNode.Composite(properties.EnumerateObject().Select(property => (property.Name, FromSchemaNode(property.Value)))),
            "array" => SensitivityNode.Collection(node.TryGetProperty("items", out var items) ? FromSchemaNode(items) : SensitivityNode.Opaque),
            "string" or "number" or "integer" or "boolean" or "null" => SensitivityNode.Leaf,
            _ => SensitivityNode.Opaque
        };
}
