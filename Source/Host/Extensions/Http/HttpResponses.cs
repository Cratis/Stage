// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Stage.Host.Invocations;

namespace Cratis.Stage.Host.Extensions.Http;

/// <summary>
/// Settles an ability from the response its request got.
/// </summary>
public static class HttpResponses
{
    /// <summary>
    /// Settles an ability from a response.
    /// </summary>
    /// <param name="definition">The <see cref="HttpAbilityDefinition"/>.</param>
    /// <param name="status">The status code of the response.</param>
    /// <param name="content">The body of the response.</param>
    /// <returns>The <see cref="AbilityResult"/>.</returns>
    public static AbilityResult Settle(HttpAbilityDefinition definition, int status, string content)
    {
        var rule = definition.Responses.FirstOrDefault(candidate => candidate.Status == status);
        if (rule is null)
        {
            // A client error other than a timeout means the system refused the request, so the effect is ruled out; anything else leaves it open.
            var ruledOut = status is >= 400 and < 500 && status != 408;
            return new AbilityFailed($"{definition.System} answered {status}, which the http block of {definition.Ability} does not map.", !ruledOut);
        }

        JsonNode? body = null;
        if (rule.Output.Any(value => value.Value.Text.StartsWith("body", StringComparison.Ordinal)) && !string.IsNullOrWhiteSpace(content))
        {
            try
            {
                body = JsonNode.Parse(content);
            }
            catch (System.Text.Json.JsonException)
            {
                return new AbilityFailed($"{definition.System} answered {status} with a body that is not JSON.");
            }
        }

        var output = new JsonObject();
        foreach (var value in rule.Output)
        {
            if (HttpValues.FromResponse(value.Value.Text, status, body) is not { } node)
            {
                return new AbilityFailed($"The {status} response of {definition.System} did not hold {value.Value.Text}, which {value.Name} is read from.");
            }

            output[value.Name] = node;
        }

        return new AbilityReturned(rule.Outcome, output.ToJsonString());
    }
}
