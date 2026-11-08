// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Keeps values of properties typed as a <c language="csharp">secret</c> concept out of what a command sends back to the caller.
/// </summary>
internal static class SemanticSecretValues
{
    internal static bool IsSecret(SemanticTypeReference type, SemanticApplication application) =>
        type.Kind == SemanticTypeReferenceKind.Concept &&
        application.Concepts.Single(concept => concept.Id == type.Target).Compliance?.Secret is not null;

    /// <summary>
    /// Omits every secret-typed property, at any depth, from a command payload that is echoed back.
    /// </summary>
    /// <param name="payload">The payload as the caller sent it.</param>
    /// <param name="properties">The command's declared properties.</param>
    /// <param name="application">The application declaring the concepts and composite types.</param>
    /// <returns>The payload itself when nothing in it is secret; otherwise a copy without the secret values.</returns>
    internal static IDictionary<string, JsonElement> Payload(IDictionary<string, JsonElement> payload, IEnumerable<SemanticProperty> properties, SemanticApplication application)
    {
        var declared = properties.ToArray();
        if (!declared.Any(property => Carries(property.Type, application, [])))
        {
            return payload;
        }

        return Members(payload.Select(member => (member.Key, member.Value)), declared, application);
    }

    /// <summary>
    /// Omits a secret-typed value from a modeled command response.
    /// </summary>
    /// <param name="response">The response the command returns.</param>
    /// <param name="declared">The response the command declares.</param>
    /// <param name="application">The application declaring the concepts.</param>
    /// <returns>The response without secret values.</returns>
    internal static object? Response(object? response, SemanticCommandResponse? declared, SemanticApplication application) => (response, declared) switch
    {
        (_, SemanticScalarCommandResponse scalar) when IsSecret(scalar.Type, application) => null,
        (Dictionary<string, object?> record, SemanticRecordCommandResponse fields) => record
            .Where(entry => !fields.Fields.Any(field => string.Equals(field.Name, entry.Key, StringComparison.OrdinalIgnoreCase) && IsSecret(field.Type, application)))
            .ToDictionary(entry => entry.Key, entry => entry.Value),
        _ => response
    };

    static bool Carries(SemanticTypeReference type, SemanticApplication application, HashSet<SemanticId> visited) =>
        IsSecret(type, application) ||
        (type.Kind == SemanticTypeReferenceKind.CompositeType && visited.Add(type.Target) &&
            application.Types.Single(candidate => candidate.Id == type.Target).Properties.Any(property => Carries(property.Type, application, visited)));

    static Dictionary<string, JsonElement> Members(IEnumerable<(string Name, JsonElement Value)> members, IReadOnlyCollection<SemanticProperty> properties, SemanticApplication application)
    {
        var result = new Dictionary<string, JsonElement>();
        foreach (var (name, value) in members)
        {
            var property = properties.FirstOrDefault(candidate => string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));
            if (property is null)
            {
                result[name] = value;
            }
            else if (!IsSecret(property.Type, application))
            {
                result[name] = Value(value, property.Type, application);
            }
        }

        return result;
    }

    static JsonElement Value(JsonElement value, SemanticTypeReference type, SemanticApplication application)
    {
        if (type.IsCollection)
        {
            return value.ValueKind == JsonValueKind.Array
                ? JsonSerializer.SerializeToElement(value.EnumerateArray().Select(item => Value(item, type with { IsCollection = false }, application)).ToArray())
                : value;
        }

        if (type.Kind == SemanticTypeReferenceKind.CompositeType && value.ValueKind == JsonValueKind.Object)
        {
            var properties = application.Types.Single(candidate => candidate.Id == type.Target).Properties;
            return JsonSerializer.SerializeToElement(Members(value.EnumerateObject().Select(member => (member.Name, member.Value)), [.. properties], application));
        }

        return value;
    }
}
