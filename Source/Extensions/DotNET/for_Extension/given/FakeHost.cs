// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Stage.Extensions.Protocol;
using Google.Protobuf;
using Grpc.Core;

namespace Cratis.Stage.Extensions.for_Extension.given;

/// <summary>
/// Stage's side of the protocol as the conformance suite fakes it: it records every call with the credential it carried and answers from canned values.
/// </summary>
/// <param name="settings">The public settings it knows.</param>
/// <param name="secrets">The secrets it knows.</param>
public sealed class FakeHost(IReadOnlyDictionary<string, string> settings, IReadOnlyDictionary<string, string> secrets) : StageHost.StageHostBase
{
    public ConcurrentQueue<(string Rpc, IMessage Request, string? Authorization)> Calls { get; } = new();

    public override Task<GetSettingResponse> GetSetting(GetSettingRequest request, ServerCallContext context)
    {
        Record("GetSetting", request, context);
        return Task.FromResult(settings.TryGetValue(request.Name, out var value)
            ? new GetSettingResponse { Status = GetSettingResponse.Types.Status.Found, Value = value }
            : new GetSettingResponse { Status = GetSettingResponse.Types.Status.NotSet });
    }

    public override Task<GetSecretResponse> GetSecret(GetSecretRequest request, ServerCallContext context)
    {
        Record("GetSecret", request, context);
        return Task.FromResult(secrets.TryGetValue(request.Name, out var value)
            ? new GetSecretResponse { Status = GetSecretResponse.Types.Status.Found, Value = value }
            : new GetSecretResponse { Status = GetSecretResponse.Types.Status.NotSet });
    }

    public override Task<RaiseTriggerResponse> RaiseTrigger(RaiseTriggerRequest request, ServerCallContext context)
    {
        Record("RaiseTrigger", request, context);
        return Task.FromResult(new RaiseTriggerResponse { Status = RaiseTriggerResponse.Types.Status.Raised });
    }

    public override Task<AnswerResponse> Answer(AnswerRequest request, ServerCallContext context)
    {
        Record("Answer", request, context);
        return Task.FromResult(new AnswerResponse { Status = AnswerResponse.Types.Status.Answered });
    }

    public override Task<TelemetryResponse> Log(LogRequest request, ServerCallContext context)
    {
        Record("Log", request, context);
        return Task.FromResult(new TelemetryResponse());
    }

    public override Task<TelemetryResponse> RecordMetrics(MetricsRequest request, ServerCallContext context)
    {
        Record("RecordMetrics", request, context);
        return Task.FromResult(new TelemetryResponse());
    }

    void Record(string rpc, IMessage request, ServerCallContext context) => Calls.Enqueue((rpc, request, context.RequestHeaders.GetValue("authorization")));
}
