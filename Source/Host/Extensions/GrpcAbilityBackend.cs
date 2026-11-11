// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Stage.Extensions.Protocol;
using Cratis.Stage.Host.Invocations;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using RequestContext = Cratis.Stage.Extensions.Protocol.RequestContext;

namespace Cratis.Stage.Host.Extensions;

/// <summary>
/// Realizes abilities by calling the extension backend over the extension protocol.
/// </summary>
/// <param name="connections">The <see cref="ExtensionConnections"/>.</param>
/// <param name="logger">The logger.</param>
public sealed class GrpcAbilityBackend(ExtensionConnections connections, ILogger<GrpcAbilityBackend> logger) : IAbilityBackend
{
    /// <summary>
    /// Gets the name of the activity source the calls to extensions are traced with.
    /// </summary>
    public const string ActivitySourceName = "Cratis.Stage.Extensions";

    static readonly ActivitySource _activities = new(ActivitySourceName);

    /// <inheritdoc/>
    public async Task<AbilityResult> Invoke(AbilityCall call, CancellationToken cancellationToken)
    {
        var system = call.Ability.System;

        // Until the handshake is done nothing was sent, so a refusal here is certain not to have reached the system.
        var result = await connections.Connect(system, cancellationToken);
        if (result.Connection is not { } connection) return new AbilityNotSent(result.Refusal);

        using var activity = _activities.StartActivity("extension.invoke", ActivityKind.Client);
        activity?.SetTag("stage.extension.system", system);
        activity?.SetTag("stage.extension.ability", call.Ability.Ability);
        activity?.SetTag("stage.invocation", call.IdempotencyKey);

        try
        {
            var response = await connection.Client.InvokeAsync(
                new InvokeRequest
                {
                    Context = Context(call, activity),
                    Ability = call.Ability.Ability,
                    ContractMajor = call.Ability.ContractMajor,
                    Effect = ExtensionContract.Effect(call.Effect),
                    InputJson = call.Input
                },
                deadline: call.Timeout > TimeSpan.Zero ? DateTime.UtcNow.Add(call.Timeout) : null,
                cancellationToken: cancellationToken);
            return Map(response);
        }
        catch (RpcException exception)
        {
            // The call may have been sent: a dropped connection or a timeout says nothing about whether the effect happened.
            ExtensionLog.InvokeFailed(logger, system, call.IdempotencyKey, exception);
            if (exception.StatusCode == StatusCode.Unavailable) connections.Forget(system);
            activity?.SetStatus(ActivityStatusCode.Error, exception.Status.Detail);
            return new AbilityFailed($"{exception.Status.StatusCode}: {exception.Status.Detail}");
        }
    }

    static RequestContext Context(AbilityCall call, Activity? activity)
    {
        var context = new RequestContext
        {
            Tenant = call.Tenant,
            System = call.Ability.System,
            InvocationId = call.IdempotencyKey,
            IdempotencyKey = call.IdempotencyKey,
            Attempt = call.Attempt,
            OriginAddress = call.Origin?.Address ?? string.Empty,
            Causation = call.Origin?.Causation ?? string.Empty,
            PackageDigest = call.PackageDigest,
            Traceparent = activity?.Id ?? string.Empty,
            Tracestate = activity?.TraceStateString ?? string.Empty
        };
        if (call.Timeout > TimeSpan.Zero) context.Deadline = Timestamp.FromDateTime(DateTime.UtcNow.Add(call.Timeout));
        return context;
    }

    static AbilityResult Map(InvokeResponse response) => response.ResultCase switch
    {
        InvokeResponse.ResultOneofCase.Returned => new AbilityReturned(
            response.Returned.Outcome.Length == 0 ? null : response.Returned.Outcome,
            response.Returned.OutputJson.Length == 0 ? "{}" : response.Returned.OutputJson),
        InvokeResponse.ResultOneofCase.Accepted => new AbilityAccepted(),
        InvokeResponse.ResultOneofCase.NotSent => new AbilityNotSent(response.NotSent.Reason),
        InvokeResponse.ResultOneofCase.Failed => new AbilityFailed(response.Failed.Reason, !response.Failed.EffectRuledOut),
        _ => new AbilityFailed("The backend answered the attempt with nothing.")
    };
}
