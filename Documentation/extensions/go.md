---
title: Write an extension backend in Go
description: "Get started with the Go SDK for Cratis Stage extension backends: declare abilities, return results, use settings, secrets, triggers and telemetry, then test it."
---

The Go SDK is the module `github.com/Cratis/Stage/Source/Extensions/Go`, at
[`Source/Extensions/Go`](https://github.com/Cratis/Stage/tree/main/Source/Extensions/Go). The package `extension` is the API; `protocol/v1` holds the
generated stubs. Read the [protocol reference](protocol.md) first for what the results mean.

## Install

```bash
go get github.com/Cratis/Stage/Source/Extensions/Go
```

A module in a subdirectory is resolved by tags of the form `Source/Extensions/Go/v1.0.0`. No such tag exists until a release cuts one, so until
then use a `replace` directive that points at a clone of [Cratis/Stage](https://github.com/Cratis/Stage).

## The smallest extension

[`Source/Extensions/Go/examples/hello/main.go`](https://github.com/Cratis/Stage/blob/main/Source/Extensions/Go/examples/hello/main.go):

```go
hello := extension.New("hello", "1.0.0").
    Setting("greeting", false).
    Ability("Greet", extension.Safe, func(ctx context.Context, call *extension.Call) (extension.Outcome, error) {
        greeting, ok, err := call.Host.Setting(ctx, "greeting")
        if err != nil {
            return extension.Outcome{}, err
        }
        if !ok {
            greeting = "Hello"
        }
        // read the name from call.Input and build the output
        return extension.Returned(`{"message":"`+greeting+`"}`, ""), nil
    })

listener, _ := net.Listen("tcp", ":50051")
log.Fatal(hello.Serve(listener))
```

Point Stage at `http://localhost:50051` ([connect a backend](connect-a-backend.md)).

## Declare what the extension realizes

Everything declared is sent at the handshake and **checked against the contract of the system**, so the effect class and the outcomes must be
what the Screenplay `system` declares.

| Call | Declares |
| --- | --- |
| `.Ability(name, effect, handler, outcomes...)` | An ability. `effect` is `extension.Safe`, `Idempotent` or `Once`. |
| `.Setting(name, required)` | A public setting read with `Host.Setting`. |
| `.Secret(name, required)` | A secret read with `Host.Secret`. |
| `.Trigger(name)` | A trigger raised with `Host.Trigger`. |
| `.HTTP(prefix, handler)` | A handler for inbound HTTP below a path prefix, such as a webhook. |
| `.Serve(listener)` / `.NewServer()` | Serves the extension, or returns the `*grpc.Server` to serve yourself. |

## Answer an attempt

A handler is `func(ctx context.Context, call *Call) (Outcome, error)`:

| Return | When |
| --- | --- |
| `extension.Returned(output, outcome)` | The system did the work. `output` is JSON; `outcome` names a declared outcome, or is empty. |
| `extension.Accepted()` | The system took the work and will answer later. Keep `call.InvocationID`. |
| `extension.NotSent(reason)` | The call certainly did not reach the system. |
| `extension.Failed(reason, effectRuledOut)` | The call failed. Pass `true` only when you can prove the effect did not happen. |

A returned `error` or a panic becomes `Failed` **without** ruling the effect out, which Stage treats as "may have happened": a `once` ability then
stops for reconciliation.

`Call` has `Tenant`, `System`, `InvocationID`, `IdempotencyKey`, `Attempt`, `Deadline`, `Causation`, `Traceparent`, `Input` and `Host`. Deduplicate your
effect on `IdempotencyKey`: it is the same on every attempt.

## Use the host services

```go
channel, ok, err := call.Host.Setting(ctx, "channel")     // public setting; ok is false when not set
token, _, err := call.Host.Secret(ctx, "token")           // secret; keep it in memory only
raised, err := call.Host.Trigger(ctx, "MessageReceived", payloadJSON, "") // an empty key is the idempotency key
status, err := call.Host.Answer(ctx, "approved", `{"by":"ada"}`, "")      // an empty id is this invocation
err = call.Host.Log(ctx, extension.Warning, "rate limit close", map[string]string{"remaining": "3"})
err = call.Host.Counter(ctx, "messages.posted", 1, nil)
err = call.Host.Histogram(ctx, "post.duration", 0.25, "s", nil)
```

`Setting` and `Secret` return an error for a name the extension did not declare or the wrong kind. `Trigger` returns a `TriggerResult` whose `Status`
is `Raised`, `UnknownTrigger`, `NotRouted` or `Refused`. Details are in the [host services reference](host-services.md).

## Regenerate the stubs

`protocol/v1` is generated from the shared `.proto` files and checked in. After the protocol changes:

```bash
go install google.golang.org/protobuf/cmd/protoc-gen-go@v1.36.11
go install google.golang.org/grpc/cmd/protoc-gen-go-grpc@latest
Source/Extensions/Go/generate.sh
```

## Test

```bash
cd Source/Extensions/Go && go test ./...
```

runs the shared [conformance suite](conformance.md) against a reference extension built with the SDK.
