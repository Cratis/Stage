// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;

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
        (_, SemanticScalarCommandResponse scalar) => Typed(response, scalar.Type, application),
        (Dictionary<string, object?> record, SemanticRecordCommandResponse fields) => record
            .Where(entry => !fields.Fields.Any(field => string.Equals(ResponseKey(field.Name), entry.Key, StringComparison.Ordinal) && IsSecret(field.Type, application)))
            .ToDictionary(
                entry => entry.Key,
                entry => Typed(entry.Value, fields.Fields.FirstOrDefault(field => string.Equals(ResponseKey(field.Name), entry.Key, StringComparison.Ordinal))?.Type, application)),
        _ => response
    };

    /// <summary>
    /// Gets the key a record response field is serialized under.
    /// </summary>
    /// <param name="name">The authored field name.</param>
    /// <returns>The response key.</returns>
    internal static string ResponseKey(string name) => JsonNamingPolicy.CamelCase.ConvertName(GeneratedPascalCase.From(name));

    // Walks a converted response value by its declared type, dropping every secret member of a composite, in lists too.
    static object? Typed(object? value, SemanticTypeReference? type, SemanticApplication application)
    {
        if (type is null || value is null)
        {
            return value;
        }

        if (IsSecret(type, application))
        {
            return null;
        }

        if (type.IsCollection && value is IEnumerable<object?> items and not IDictionary<string, object?>)
        {
            return items.Select(item => Typed(item, type with { IsCollection = false }, application)).ToArray();
        }

        if (type.Kind != SemanticTypeReferenceKind.CompositeType || value is not IDictionary<string, object?> composite)
        {
            return value;
        }

        var properties = application.Types.Single(candidate => candidate.Id == type.Target).Properties;
        var result = new Dictionary<string, object?>();
        foreach (var (name, member) in composite)
        {
            var property = properties.FirstOrDefault(candidate => string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));
            if (property is not null && !IsSecret(property.Type, application))
            {
                result[name] = Typed(member, property.Type, application);
            }
        }

        return result;
    }

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
            else if (!IsSecret(property.Type, application) && Value(value, property.Type, application) is { } kept)
            {
                result[name] = kept;
            }
        }

        return result;
    }

    // A value of the wrong shape at a position that can hold secrets cannot be filtered, so it is dropped (null).
    static JsonElement? Value(JsonElement value, SemanticTypeReference type, SemanticApplication application)
    {
        var carries = Carries(type, application, []);
        if (type.IsCollection)
        {
            if (value.ValueKind != JsonValueKind.Array)
            {
                return carries ? null : value;
            }

            var kept = value.EnumerateArray().Select(item => Value(item, type with { IsCollection = false }, application)).ToArray();
            return kept.Any(item => item is null) ? null : JsonSerializer.SerializeToElement(kept);
        }

        if (type.Kind == SemanticTypeReferenceKind.CompositeType && carries)
        {
            if (value.ValueKind != JsonValueKind.Object)
            {
                return value.ValueKind == JsonValueKind.Null ? value : null;
            }

            var properties = application.Types.Single(candidate => candidate.Id == type.Target).Properties;
            return JsonSerializer.SerializeToElement(Members(value.EnumerateObject().Select(member => (member.Name, member.Value)), [.. properties], application));
        }

        return value;
    }
}
