// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Host.Invocations;

namespace Cratis.Stage.Host.Extensions.Http;

/// <summary>
/// The built-in <c lang="text">http</c> extension: realizes the abilities that declare an <c lang="text">http</c> block, in-process, through the same durable invocation path as any other extension,
/// and hands every other ability to the backend it decorates.
/// </summary>
/// <param name="inner">The <see cref="IAbilityBackend"/> that realizes every other ability.</param>
/// <param name="definitions">The <see cref="IHttpAbilityDefinitions"/>.</param>
/// <param name="access">The <see cref="HttpSystemAccess"/>.</param>
/// <param name="clients">The <see cref="IHttpClientFactory"/>.</param>
/// <param name="logger">The logger.</param>
public sealed class HttpRealizingAbilityBackend(
    IAbilityBackend inner,
    IHttpAbilityDefinitions definitions,
    HttpSystemAccess access,
    IHttpClientFactory clients,
    ILogger<HttpRealizingAbilityBackend> logger) : IAbilityBackend
{
    /// <summary>
    /// The name of the <see cref="HttpClient"/> the extension uses.
    /// </summary>
    public const string ClientName = "Cratis.Stage.Extensions.Http";

    /// <inheritdoc/>
    public async Task<AbilityResult> Invoke(AbilityCall call, CancellationToken cancellationToken)
    {
        var system = call.Ability.System;
        var ability = call.Ability.Ability;
        if (definitions.Find(system, ability) is not { } definition) return await inner.Invoke(call, cancellationToken);

        // Nothing is sent until the request is built and the system is reachable with its own credentials, so a refusal up to here is certain not to have reached it.
        var connection = access.Resolve(system);
        if (connection.BaseAddress is null) return NotSent(system, ability, connection.Refusal!);

        HttpRequestMessage request;
        try
        {
            request = HttpRequests.Build(definition, call, connection.BaseAddress);
        }
        catch (HttpRequestFailure failure)
        {
            return NotSent(system, ability, failure.Message);
        }
        catch (System.Text.Json.JsonException)
        {
            return NotSent(system, ability, "The input of the call is not JSON.");
        }

        using (request)
        {
            connection.Authenticate(request);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (call.Timeout > TimeSpan.Zero) timeout.CancelAfter(call.Timeout);

            try
            {
                using var response = await clients.CreateClient(ClientName).SendAsync(request, HttpCompletionOption.ResponseContentRead, timeout.Token);
                var content = await response.Content.ReadAsStringAsync(timeout.Token);
                var status = (int)response.StatusCode;
                HttpAbilityLog.Answered(logger, system, ability, status, call.IdempotencyKey);
                return HttpResponses.Settle(definition, status, content);
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or IOException && !cancellationToken.IsCancellationRequested)
            {
                // The request may have been sent: a dropped connection or a timeout says nothing about whether the effect happened. Only the type is reported, as a message can hold an address.
                HttpAbilityLog.DidNotComplete(logger, system, ability, call.IdempotencyKey, exception.GetType().Name);
                return new AbilityFailed($"The request to {system} did not complete: {exception.GetType().Name}.");
            }
        }
    }

    AbilityNotSent NotSent(string system, string ability, string reason)
    {
        HttpAbilityLog.NotSent(logger, system, ability, reason);
        return new(reason);
    }
}
