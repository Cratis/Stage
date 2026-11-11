---
title: Extension SDK conformance suite
description: The shared conformance suite every Cratis Stage extension SDK passes - the reference extension each SDK builds, the cases it runs over the wire, and how to add an SDK for another language.
---

Four SDKs implement one protocol, so one suite holds them to the same behavior. It is
[`Source/Extensions/Conformance`](https://github.com/Cratis/Stage/tree/main/Source/Extensions/Conformance): a language-neutral file of cases,
`cases.json`, and the description of a **reference extension** that each SDK implements in its own language.

## How a run works

Each SDK's own test suite, using its own tooling (`dotnet test`, `go test`, `gradle test`, `npm test`):

1. builds the reference extension with the SDK and starts it on a loopback port;
2. starts a fake `StageHost` that records every call with the `authorization` header it carried;
3. reads `cases.json` and for each case calls the extension over gRPC, then compares the response and the recorded host calls.

Nothing is mocked in between: the test crosses the wire the way Stage does. The fake host also proves that **every host call presents the session
credential** the handshake handed over.

A response or a host call matches when everything the case lists is present: objects by key, arrays by index and length, scalars by value. A
field whose name ends in `Json` is compared as JSON, so key order does not matter. The request is written in the JSON mapping of the protocol
(camelCase fields, enum names, bytes as base64, timestamps as RFC 3339); `@host` in `hostEndpoint` stands for the fake host's address.

## The reference extension

Named `conformance-extension`, version `1.0.0`, system `Conformance`.

| Ability | Effect | Does |
| --- | --- | --- |
| `Echo` | safe | Logs `echoing` at information level with attribute `size` (the input's length in bytes), adds 1 to counter `echo.calls`, and returns the input as its output. |
| `Greet` | idempotent, outcome `greeted` | Reads setting `greeting` and secret `apiKey` through the host and returns output `{"greeting":…,"secretLength":…}` with outcome `greeted`. |
| `Context` | safe | Returns the tenant, system, invocation id, idempotency key, attempt, causation and traceparent it received, as JSON. |
| `Notify` | once | Raises trigger `Notified` with the input as the payload and the idempotency key as the deduplication key, then returns. |
| `Defer` | once, outcome `done` | Returns `Accepted`, then about 50 ms later calls `Answer` with outlet `done` and payload `{"ok":true}` for the same invocation. |
| `Refuse` | idempotent | Returns `NotSent("refused by design")`. |
| `Reject` | once | Returns `Failed("rejected by the system")` with `effect_ruled_out` true. |
| `Crash` | once | Throws, which the SDK turns into `Failed("boom")` without ruling the effect out. |

Settings: `greeting` (required) and secret `apiKey` (required). Trigger: `Notified`. HTTP route `/hook`: `POST /hook/events` answers 202 with body
`received:` followed by the request body and header `X-Echo` set to the query string; any other path under `/hook` answers 404.

An ability the extension does not realize answers `NotSent`.

Abilities, triggers, settings and routes are described **in declaration order**, in the order above.

## What the cases cover

- The handshake: the host endpoint and credential are accepted, and the extension describes itself completely.
- Every result kind: returned (plain and named), accepted, not sent, failed with and without the effect ruled out, and an exception.
- Every request carries the context to the handler.
- Settings and secrets through the host, with the credential on each call.
- Raising a trigger deduplicated on the idempotency key, and answering later by the invocation identity.
- Logs and metrics tied to the invocation.
- Inbound HTTP, including an unhandled path, and health.

## Add an SDK for another language

1. Generate or load the stubs from the [`.proto` files](https://github.com/Cratis/Stage/tree/main/Source/Extensions/Protocol).
2. Implement the extension side: `Describe`, `Invoke`, `HandleHttp` and `Health`, a client of `StageHost` created at the handshake that
   presents the credential on every call, and the helpers authors use.
3. Implement the reference extension in your SDK's tests, and a small harness that runs `cases.json` as above.
4. Make the extension turn an unexpected error into `Failed` with the effect **not** ruled out, never into a protocol error.
5. Add a minimal example and an authoring page beside the existing ones, and a job to the
   [extension SDKs workflow](https://github.com/Cratis/Stage/blob/main/.github/workflows/extension-sdks.yml).

## See also

- [Protocol reference](protocol.md)
- [Host services reference](host-services.md)
