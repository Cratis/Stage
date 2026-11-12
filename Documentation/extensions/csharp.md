---
title: Write an extension backend in C#
description: Getting started with the Cratis.Stage.Extensions SDK - declare abilities, return the right result, use settings, secrets, triggers, answers and telemetry, run the backend and test it.
---

The C# SDK is the package `Cratis.Stage.Extensions` (`net10.0` and `net11.0`), at
[`Source/Extensions/DotNET`](https://github.com/Cratis/Stage/tree/main/Source/Extensions/DotNET). It wraps the generated gRPC stubs so a backend is a
few declarations. Read the [protocol reference](protocol.md) first for what the results mean.

## Install

```bash
dotnet add package Cratis.Stage.Extensions
```

The package is built and packed with the rest of the repository. Until a release carries it, reference the project from a clone of
[Cratis/Stage](https://github.com/Cratis/Stage).

## The smallest extension

[`Source/Extensions/DotNET/Example`](https://github.com/Cratis/Stage/tree/main/Source/Extensions/DotNET/Example) is a complete project:

```csharp
using System.Net;
using System.Text.Json;
using Cratis.Stage.Extensions;

var extension = Extension.Create("hello", "1.0.0")
    .Setting("greeting")
    .Ability("Greet", EffectClass.Safe, async call =>
    {
        var greeting = await call.Host.Setting("greeting", call.CancellationToken) ?? "Hello";
        var name = JsonElement.Parse(call.Input).GetProperty("name").GetString();
        return AbilityOutcome.Returned(JsonSerializer.Serialize(new { message = $"{greeting}, {name}!" }));
    })
    .Build();

await ExtensionRunner.Run(extension, new IPEndPoint(IPAddress.Any, 50051));
```

Run it, then point Stage at it with `Stage:Extensions:Backends:Hello:Endpoint = http://localhost:50051`
([connect a backend](connect-a-backend.md)).

## Declare what the extension realizes

`Extension.Create(name, version)` starts a description. Everything declared here is sent at the handshake and **checked against the contract of the
system**, so the effect class and the outcomes must be what the Screenplay `system` declares.

| Call | Declares |
| --- | --- |
| `.Ability(name, effect, handler, outcomes?, contractMajor = 1)` | An ability. `effect` is `EffectClass.Safe`, `Idempotent` or `Once`. |
| `.Setting(name, required?)` | A public setting read with `Host.Setting`. |
| `.Secret(name, required?)` | A secret read with `Host.Secret`. |
| `.Trigger(name)` | A trigger raised with `Host.Trigger`. |
| `.Http(prefix, handler)` | A handler for inbound HTTP below a path prefix, such as a webhook. |
| `.Build()` | The `ExtensionDefinition`. |

## Answer an attempt

A handler receives an `AbilityContext` and returns an `AbilityOutcome`:

| Return | When |
| --- | --- |
| `AbilityOutcome.Returned(outputJson, outcome?)` | The system did the work. `outcome` names a declared outcome. |
| `AbilityOutcome.Accepted()` | The system took the work and will answer later. Keep `call.InvocationId`. |
| `AbilityOutcome.NotSent(reason)` | The call certainly did not reach the system. |
| `AbilityOutcome.Failed(reason, effectRuledOut: false)` | The call failed. Set `effectRuledOut` only when you can prove the effect did not happen. |

An exception thrown by the handler becomes `Failed` **without** ruling the effect out, which is what Stage treats as "may have happened": a `once`
ability then stops for reconciliation. Catch the errors you can classify and return `NotSent` or `Failed(..., effectRuledOut: true)` for them.

The context carries `Tenant`, `System`, `InvocationId`, `IdempotencyKey`, `Attempt`, `Deadline`, `Causation`, `Traceparent`, `Input` and a
`CancellationToken`. Deduplicate your effect on `IdempotencyKey`: it is the same on every attempt.

## Use the host services

`call.Host` is tied to the invocation:

```csharp
var channel = await call.Host.Setting("channel", call.CancellationToken);     // public setting, null when not set
var token = await call.Host.Secret("token", call.CancellationToken);          // secret; keep it in memory only
var raised = await call.Host.Trigger("MessageReceived", payloadJson);         // dedup key defaults to the idempotency key
await call.Host.Answer("approved", "{\"by\":\"ada\"}");                       // complete an accepted invocation later
await call.Host.Log(LogLevel.Warning, "rate limit close", new Dictionary<string, string> { ["remaining"] = "3" });
await call.Host.Counter("messages.posted");
await call.Host.Histogram("post.duration", 0.25, "s");
```

`Setting` and `Secret` throw `HostServiceException` for a name the extension did not declare or the wrong kind. `Trigger` returns a
`TriggerResult` whose `Status` is `Raised`, `UnknownTrigger`, `NotRouted` or `Refused`. To answer from somewhere else, such as an HTTP handler,
pass the identity: `Answer(outlet, payload, invocationId)`. Details are in the [host services reference](host-services.md).

## Handle inbound HTTP

Stage stores a `POST`, `PUT`, `PATCH` or `DELETE`, acknowledges the sender and then delivers it to your handler, activating your backend when it is not running and
retrying when it fails. Read the full contract in [inbound traffic and webhooks](inbound-traffic.md). The call carries the raw body, so a signature verifies,
and a `DeliveryId` that is the same for every attempt, so you can deduplicate on it.

```csharp
.Http("/webhook", async (call, cancellationToken) =>
{
    var secret = await host.Secret("SigningSecret", cancellationToken);
    var signature = call.Headers.TryGetValue("X-Signature", out var values) ? values.FirstOrDefault() : null;

    // The endpoint is anonymous: verify first. A 401 makes Stage reject the request for good instead of retrying it.
    if (!WebhookSignature.Verify(secret ?? string.Empty, call.Body.Span, signature)) return new HttpReply(401);

    var evt = JsonSerializer.Deserialize<PaymentEvent>(call.Body.Span)!;
    await host.Trigger("PaymentSucceeded", payloadJson, deduplicationKey: evt.Id, correlationKey: evt.PaymentId, cancellationToken: cancellationToken);
    return new HttpReply(200);
})
```

## Verify a webhook signature

`WebhookSignature` verifies an HMAC-SHA256 signature as lowercase hexadecimal, with or without a `sha256=` prefix, in constant time. An empty secret, a missing
header or a signature of the wrong length verifies nothing.

| Call | Verifies |
| --- | --- |
| `WebhookSignature.Verify(secret, body, signature)` | A signature over the raw body, as GitHub sends it. |
| `WebhookSignature.Verify(secret, body, timestamp, signature, now, tolerance)` | A signature over `<timestamp>.<body>`, as Stripe sends it, and refuses a timestamp more than `tolerance` from `now`. Use it to refuse a captured request replayed later. |
| `WebhookSignature.Compute(secret, payload)` | The signature, for a test that sends a signed request. |

Read the secret with `host.Secret(...)` from a setting the contract declares as secret; never put it in the model or the code. See [security in inbound
traffic](inbound-traffic.md#security) for what Stage does about replays and which identity a reaction runs as.

## Run and test

`ExtensionRunner.Run(definition, endpoint)` runs a Kestrel gRPC server until cancelled. `ExtensionRunner.Build` returns the `WebApplication` without
starting it, with port `0` to let the system choose. The SDK's own tests run the shared [conformance suite](conformance.md):

```bash
dotnet test Source/Extensions/DotNET/Cratis.Stage.Extensions.csproj
```

To test your backend the same way, start it with `ExtensionRunner.Build`, and call it with a client generated from the protocol.
