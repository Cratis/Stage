// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Stage.Extensions.Protocol;
using Cratis.Stage.Host.Invocations;
using Grpc.Core;
using Microsoft.Extensions.Options;
using RequestContext = Cratis.Stage.Extensions.Protocol.RequestContext;

namespace Cratis.Stage.Host.Extensions;

/// <summary>
/// Stage's side of the extension protocol: the common services an extension asks for. Every call names the system through the credential the handshake handed over.
/// </summary>
/// <param name="sessions">The <see cref="ExtensionSessions"/>.</param>
/// <param name="settings">The <see cref="ISystemSettings"/>.</param>
/// <param name="triggers">The <see cref="IContractTriggers"/>.</param>
/// <param name="invocations">The <see cref="IAbilityInvocations"/>.</param>
/// <param name="telemetry">The <see cref="ExtensionTelemetry"/>.</param>
/// <param name="logger">The logger.</param>
public sealed class StageHostService(
    ExtensionSessions sessions,
    ISystemSettings settings,
    IContractTriggers triggers,
    IAbilityInvocations invocations,
    ExtensionTelemetry telemetry,
    ILogger<StageHostService> logger) : StageHost.StageHostBase
{
    /// <inheritdoc/>
    public override Task<GetSettingResponse> GetSetting(GetSettingRequest request, ServerCallContext context)
    {
        var (system, description) = Caller(context);
        if (description.Settings.FirstOrDefault(setting => setting.Name == request.Name) is not { } declared)
        {
            return Task.FromResult(new GetSettingResponse { Status = GetSettingResponse.Types.Status.Unknown });
        }

        if (declared.Secret) return Task.FromResult(new GetSettingResponse { Status = GetSettingResponse.Types.Status.IsSecret });

        var value = settings.Resolve(system, request.Name);
        return Task.FromResult(value is null
            ? new GetSettingResponse { Status = GetSettingResponse.Types.Status.NotSet }
            : new GetSettingResponse { Status = GetSettingResponse.Types.Status.Found, Value = value });
    }

    /// <inheritdoc/>
    public override Task<GetSecretResponse> GetSecret(GetSecretRequest request, ServerCallContext context)
    {
        var (system, description) = Caller(context);
        if (description.Settings.FirstOrDefault(setting => setting.Name == request.Name) is not { } declared)
        {
            return Task.FromResult(new GetSecretResponse { Status = GetSecretResponse.Types.Status.Unknown });
        }

        if (!declared.Secret) return Task.FromResult(new GetSecretResponse { Status = GetSecretResponse.Types.Status.NotSecret });

        var value = settings.Resolve(system, request.Name);
        return Task.FromResult(value is null
            ? new GetSecretResponse { Status = GetSecretResponse.Types.Status.NotSet }
            : new GetSecretResponse { Status = GetSecretResponse.Types.Status.Found, Value = value });
    }

    /// <inheritdoc/>
    public override async Task<RaiseTriggerResponse> RaiseTrigger(RaiseTriggerRequest request, ServerCallContext context)
    {
        var (system, description) = Caller(context);
        if (!description.Triggers.Contains(request.Trigger))
        {
            return new RaiseTriggerResponse { Status = RaiseTriggerResponse.Types.Status.UnknownTrigger, Detail = $"{system} declares no trigger {request.Trigger}." };
        }

        var raised = await triggers.Signal(new TriggerRaise(
            system,
            request.Trigger,
            request.Context?.Tenant ?? string.Empty,
            request.DeduplicationKey,
            request.PayloadJson,
            request.Context?.InvocationId is { Length: > 0 } invocation ? invocation : $"extension:{system}"));
        return new RaiseTriggerResponse
        {
            Status = raised.Status switch
            {
                TriggerRaiseStatus.Raised => RaiseTriggerResponse.Types.Status.Raised,
                TriggerRaiseStatus.NotRouted => RaiseTriggerResponse.Types.Status.NotRouted,
                _ => RaiseTriggerResponse.Types.Status.Refused
            },
            Detail = raised.Detail
        };
    }

    /// <inheritdoc/>
    public override async Task<AnswerResponse> Answer(AnswerRequest request, ServerCallContext context)
    {
        var (system, _) = Caller(context);
        var principal = new ClaimsPrincipal(new ExtensionIdentity(system));
        var answered = await invocations.Answer(request.Context?.InvocationId ?? string.Empty, request.Outlet, request.PayloadJson.Length == 0 ? "{}" : request.PayloadJson, principal);
        return new AnswerResponse
        {
            Status = answered switch
            {
                InvocationAnswer.Answered => AnswerResponse.Types.Status.Answered,
                InvocationAnswer.NotWaiting => AnswerResponse.Types.Status.NotWaiting,
                InvocationAnswer.Forbidden => AnswerResponse.Types.Status.Forbidden,
                InvocationAnswer.UnknownOutlet => AnswerResponse.Types.Status.UnknownOutlet,
                _ => AnswerResponse.Types.Status.UnknownInvocation
            }
        };
    }

    /// <inheritdoc/>
    public override Task<TelemetryResponse> Log(LogRequest request, ServerCallContext context)
    {
        var (system, _) = Caller(context);
        telemetry.Log(system, request.Context ?? new RequestContext(), request.Records);
        return Task.FromResult(new TelemetryResponse());
    }

    /// <inheritdoc/>
    public override Task<TelemetryResponse> RecordMetrics(MetricsRequest request, ServerCallContext context)
    {
        var (system, _) = Caller(context);
        telemetry.Record(system, request.Context ?? new RequestContext(), request.Metrics);
        return Task.FromResult(new TelemetryResponse());
    }

    (string System, ExtensionDescription Description) Caller(ServerCallContext context)
    {
        var header = context.RequestHeaders.GetValue("authorization");
        if (header?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) != true || sessions.SystemOf(header["Bearer ".Length..].Trim()) is not { } system)
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "The call carries no valid session credential."));
        }

        if (sessions.Description(system) is not { } description)
        {
            ExtensionLog.CallRefused(logger, system, "the handshake is not complete");
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "The handshake has not completed. Call the host services after Describe returns."));
        }

        return (system, description);
    }
}
