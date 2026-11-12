// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Stage.Host.Invocations;

namespace Cratis.Stage.Host.Extensions.Http;

/// <summary>
/// Reads the values of a block from the call and from the response.
/// </summary>
public static class HttpValues
{
    /// <summary>
    /// Reads a value of the call.
    /// </summary>
    /// <param name="value">The <see cref="HttpValue"/>.</param>
    /// <param name="call">The <see cref="AbilityCall"/>.</param>
    /// <param name="input">The input of the ability.</param>
    /// <returns>The value, or null when an optional input is absent.</returns>
    public static JsonNode? FromCall(HttpValue value, AbilityCall call, JsonNode? input) => value.Kind switch
    {
        HttpValueKind.Literal => value.Literal?.DeepClone(),
        HttpValueKind.Call => value.Text switch
        {
            "idempotencyKey" => JsonValue.Create(call.IdempotencyKey),
            "attempt" => JsonValue.Create(call.Attempt),
            "tenant" => JsonValue.Create(call.Tenant),
            "system" => JsonValue.Create(call.Ability.System),
            _ => JsonValue.Create(call.Ability.Ability)
        },
        _ => Walk(input, value.Text)?.DeepClone()
    };

    /// <summary>
    /// Reads a value of a response.
    /// </summary>
    /// <param name="path">The path, <c lang="text">status</c> or starting with <c lang="text">body</c>.</param>
    /// <param name="status">The status code.</param>
    /// <param name="body">The parsed body, or null when there is none.</param>
    /// <returns>The value, or null when the response does not hold it.</returns>
    public static JsonNode? FromResponse(string path, int status, JsonNode? body)
    {
        if (path == "status") return JsonValue.Create(status);
        return (path == "body" ? body : Walk(body, path["body.".Length..]))?.DeepClone();
    }

    /// <summary>
    /// Writes a value as text for a header, a query parameter or a path.
    /// </summary>
    /// <param name="node">The value.</param>
    /// <returns>The text.</returns>
    public static string AsText(JsonNode node) =>
        node is JsonValue value && value.TryGetValue<string>(out var text) ? text : node.ToJsonString();

    static JsonNode? Walk(JsonNode? node, string path)
    {
        foreach (var segment in path.Split('.'))
        {
            if (node is not JsonObject @object || !@object.TryGetPropertyValue(segment, out node)) return null;
        }

        return node;
    }
}
