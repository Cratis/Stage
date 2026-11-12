// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Stage.Host.Invocations;

namespace Cratis.Stage.Host.Extensions.Http;

/// <summary>
/// Builds the HTTP request of an ability from its block and its call.
/// </summary>
public static class HttpRequests
{
    /// <summary>
    /// Builds a request. Credentials are added by the caller, from the settings of the system.
    /// </summary>
    /// <param name="definition">The <see cref="HttpAbilityDefinition"/>.</param>
    /// <param name="call">The <see cref="AbilityCall"/>.</param>
    /// <param name="baseAddress">The base address of the system, ending in a slash.</param>
    /// <returns>The request.</returns>
    /// <exception cref="HttpRequestFailure">Thrown when the call cannot fill the request; nothing was sent.</exception>
    public static HttpRequestMessage Build(HttpAbilityDefinition definition, AbilityCall call, Uri baseAddress)
    {
        var input = string.IsNullOrWhiteSpace(call.Input) ? null : JsonNode.Parse(call.Input);

        var path = definition.Path;
        foreach (var name in Placeholders(path))
        {
            var value = HttpValues.FromCall(new HttpValue(HttpValueKind.Input, name), call, input)
                ?? throw new HttpRequestFailure($"The input {name} that the path of {call.Ability.Ability} needs is absent.");
            path = path.Replace("{" + name + "}", Uri.EscapeDataString(HttpValues.AsText(value)), StringComparison.Ordinal);
        }

        var query = new StringBuilder();
        foreach (var parameter in definition.Query)
        {
            if (HttpValues.FromCall(parameter.Value, call, input) is not { } value) continue;
            query.Append(query.Length == 0 ? '?' : '&').Append(Uri.EscapeDataString(parameter.Name)).Append('=').Append(Uri.EscapeDataString(HttpValues.AsText(value)));
        }

        var target = new Uri(baseAddress, path.TrimStart('/') + query);
        if (target.Host != baseAddress.Host || target.Port != baseAddress.Port || target.Scheme != baseAddress.Scheme || !target.AbsolutePath.StartsWith(baseAddress.AbsolutePath, StringComparison.Ordinal))
        {
            throw new HttpRequestFailure("The request would leave the base address of the system.");
        }

        var request = new HttpRequestMessage(new HttpMethod(definition.Method), target);
        foreach (var header in definition.Headers)
        {
            if (HttpValues.FromCall(header.Value, call, input) is { } value) request.Headers.TryAddWithoutValidation(header.Name, HttpValues.AsText(value));
        }

        if (definition.Body.Count > 0)
        {
            request.Content = new StringContent(Body(definition.Body, call, input).ToJsonString(), Encoding.UTF8, "application/json");
        }

        return request;
    }

    static JsonObject Body(IReadOnlyList<HttpBodyField> fields, AbilityCall call, JsonNode? input)
    {
        var body = new JsonObject();
        foreach (var field in fields)
        {
            if (field.Value is not null)
            {
                if (HttpValues.FromCall(field.Value, call, input) is { } value) body[field.Name] = value;
            }
            else
            {
                body[field.Name] = Body(field.Fields, call, input);
            }
        }

        return body;
    }

    static IEnumerable<string> Placeholders(string path)
    {
        var start = path.IndexOf('{', StringComparison.Ordinal);
        while (start >= 0)
        {
            var end = path.IndexOf('}', start);
            if (end < 0) yield break;
            yield return path[(start + 1)..end];
            start = path.IndexOf('{', end);
        }
    }
}
