// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Cratis.Stage.Extensions.for_Extension.given;

/// <summary>
/// The reference extension of the conformance suite, written with the C# SDK. Every SDK implements the same behavior; see Source/Extensions/Conformance/README.md.
/// </summary>
public static class ConformanceExtension
{
    public static ExtensionDefinition Create() => Extension.Create("conformance-extension", "1.0.0")
        .Setting("greeting", required: true)
        .Secret("apiKey", required: true)
        .Trigger("Notified")
        .Ability("Echo", EffectClass.Safe, async call =>
        {
            var size = Encoding.UTF8.GetByteCount(call.Input).ToString(System.Globalization.CultureInfo.InvariantCulture);
            await call.Host.Log(LogLevel.Information, "echoing", new Dictionary<string, string> { ["size"] = size }, call.CancellationToken);
            await call.Host.Counter("echo.calls", 1, cancellationToken: call.CancellationToken);
            return AbilityOutcome.Returned(call.Input);
        })
        .Ability("Greet", EffectClass.Idempotent, Greet, ["greeted"])
        .Ability("Context", EffectClass.Safe, call => Task.FromResult(AbilityOutcome.Returned(Describe(call))))
        .Ability("Notify", EffectClass.Once, async call =>
        {
            await call.Host.Trigger("Notified", call.Input, cancellationToken: call.CancellationToken);
            return AbilityOutcome.Returned();
        })
        .Ability("Defer", EffectClass.Once, Defer, ["done"])
        .Ability("Refuse", EffectClass.Idempotent, _ => Task.FromResult(AbilityOutcome.NotSent("refused by design")))
        .Ability("Reject", EffectClass.Once, _ => Task.FromResult(AbilityOutcome.Failed("rejected by the system", effectRuledOut: true)))
        .Ability("Crash", EffectClass.Once, _ => throw new InvalidOperationException("boom"))
        .Http("/hook", (call, _) => Task.FromResult(call.Path == "/hook/events"
            ? new HttpReply(202, Encoding.UTF8.GetBytes("received:" + Encoding.UTF8.GetString(call.Body.Span)), new Dictionary<string, string[]> { ["X-Echo"] = [call.Query] })
            : new HttpReply(404)))
        .Build();

    static async Task<AbilityOutcome> Greet(AbilityContext call)
    {
        var greeting = await call.Host.Setting("greeting", call.CancellationToken);
        var key = await call.Host.Secret("apiKey", call.CancellationToken);
        return AbilityOutcome.Returned(new JsonObject { ["greeting"] = greeting, ["secretLength"] = key?.Length }.ToJsonString(), "greeted");
    }

    static Task<AbilityOutcome> Defer(AbilityContext call)
    {
        // Accept now, answer later: the answer is given after the attempt has been acknowledged.
        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            await call.Host.Answer("done", "{\"ok\":true}");
        });
        return Task.FromResult(AbilityOutcome.Accepted());
    }

    static string Describe(AbilityContext call) => new JsonObject
    {
        ["tenant"] = call.Tenant,
        ["system"] = call.System,
        ["invocationId"] = call.InvocationId,
        ["idempotencyKey"] = call.IdempotencyKey,
        ["attempt"] = call.Attempt,
        ["causation"] = call.Causation,
        ["traceparent"] = call.Traceparent
    }.ToJsonString();
}
