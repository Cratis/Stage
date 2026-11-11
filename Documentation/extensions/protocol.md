---
title: Extension protocol reference
description: The Cratis Stage extension protocol, cratis.stage.extensions.v1 - the two gRPC services, every message, the rules for results, retries and deadlines, and how the protocol is versioned.
---

The extension protocol is defined in three `.proto` files under
[`Source/Extensions/Protocol/cratis/stage/extensions/v1`](https://github.com/Cratis/Stage/tree/main/Source/Extensions/Protocol/cratis/stage/extensions/v1).
They are the single source of truth: every SDK and Stage itself generate or load their code from these files.

| File | Holds |
| --- | --- |
| `common.proto` | `RequestContext`, `EffectClass`, `ProtocolVersion`, `Header` |
| `backend.proto` | `service ExtensionBackend`, which Stage calls and the extension implements |
| `host.proto` | `service StageHost`, which the extension calls and Stage implements |

Package `cratis.stage.extensions.v1`. Transport is gRPC over HTTP/2.

## Who connects to whom

The extension **listens**; Stage **connects** to it, at an address from Stage's configuration (see
[Connect a backend to Stage](connect-a-backend.md)). Stage listens too, on a separate port for the host services, and tells the extension where in
the first message it sends. So the protocol is two-way without either side needing a long-lived stream: both directions are plain unary calls.

## The handshake

`ExtensionBackend.Describe` is always the first call. Stage sends:

| Field | Meaning |
| --- | --- |
| `protocol_version` | The version Stage speaks. |
| `system`, `package_digest` | The system the backend serves and the locked digest of its package, empty for a backend that runs by itself. |
| `host_endpoint` | Where `StageHost` listens, such as `http://stage:50100`. |
| `session_token` | The credential the extension presents to `StageHost`. |

The extension keeps the endpoint and token **in memory** and answers with what it realizes: its name and version, its `abilities` (name, contract
major, effect class, outcomes), the `triggers` it may raise, the `settings` it reads (each marked `secret` or not, and `required` or not) and the
`http_routes` it handles.

Stage then **checks the running backend against the contract of its system**. A backend that realizes less than the contract, or realizes an
ability with another effect class, another contract major or other outcomes, is refused: no attempt is sent to it, so the answer for the
invocation is `AbilityNotSent`, which is safe to retry. A backend may realize more than the contract declares.

A failed handshake is not remembered. The next attempt does the handshake again, which is also what happens after a backend restarts.

## Every request carries a `RequestContext`

| Field | Meaning |
| --- | --- |
| `tenant` | The tenant the work runs for. Stage runs one scope, `Default`, today. |
| `system` | The system whose ability is performed. |
| `invocation_id` | The invocation identity. Empty outside an invocation. |
| `idempotency_key` | The key the extension deduplicates its effect on. It is the same for every attempt of an invocation. |
| `attempt` | The attempt number, starting at 1. Zero outside an invocation. |
| `deadline` | When the attempt runs out of time. Unset means none is imposed beyond the transport's. |
| `traceparent`, `tracestate` | The W3C trace context, so the extension's own telemetry joins the invocation's trace. |
| `origin_address`, `causation` | What caused the invocation: the address of the invoking use in the model, and the occurrence. |
| `package_digest` | The package the invocation pinned. |

An extension passes the context it received back on every `StageHost` call it makes for that invocation. Stage uses it to tie logs, metrics and
triggers to the invocation's causation.

## Performing an ability: `Invoke`

`InvokeRequest` carries the `context`, the `ability` name and `contract_major`, the `effect` class and the `input_json`. The extension answers with
exactly one result:

| Result | Say it when | What Stage does |
| --- | --- | --- |
| `Returned { outcome, output_json }` | The system did the work. `outcome` is empty for a plain return, or a named outcome the ability declares. | Continues the origin through that outlet. |
| `Accepted` | The system took the work and will answer later. | Suspends the invocation without holding a worker until `Answer` arrives or the answer expires. |
| `NotSent { reason }` | The call **certainly did not reach the system**. | Retries the attempt, even for a `once` ability. |
| `Failed { reason, effect_ruled_out }` | The call failed. | By default assumes the effect may have happened. `effect_ruled_out = true` says the extension can rule that out. |

### The rule that matters most

Do not claim a certainty you do not have. `effect_ruled_out` defaults to `false` on the wire, which means "the effect may have happened". That
is what a thrown exception, a timeout and a dropped connection mean. For a `once` ability Stage then **stops the invocation for reconciliation**
instead of calling again, because a second attempt could repeat the effect. Say `NotSent` or `effect_ruled_out` only when the system could not
have acted, such as a request refused locally before it was sent.

Stage applies the same rule to its own side of the connection:

| What happens | Answer Stage records |
| --- | --- |
| No backend is known, the handshake fails or the contract is not honored | `NotSent`: nothing was sent. |
| The backend answers | What it answered. |
| An error or a timeout after the call was sent | `Failed`, may have reached the system. |
| The backend is unreachable after the handshake once succeeded | `Failed`, may have reached the system, and the connection is dropped so the next attempt does the handshake again. |

### Deadlines and idempotency

`context.deadline` is when the attempt runs out of time. Stage also sets it as the gRPC deadline of the call. Check
`context.attempt` and `context.idempotency_key`: for an `idempotent` or `once` ability the same key arrives on every attempt, and the same call
must have the same effect. Pass the key to the outside system when it supports one.

## Inbound HTTP: `HandleHttp`

An extension can own a webhook or a callback. Stage routes `/extensions/<System>/<path>` to the backend **when `<path>` starts with a route
the backend declared in `http_routes`**, on a path boundary: `/hook` handles `/hook` and `/hook/events`, not `/hooks`. Anything else is a 404 from
Stage, or a rejection at delivery when the extension was not running to say, as is a system with no backend.

How the traffic reaches `HandleHttp` is described in [inbound traffic and webhooks](inbound-traffic.md). In short, a `POST`, `PUT`, `PATCH` or `DELETE` is
**stored and acknowledged with `202` first**, then delivered with retries; a `GET`, `HEAD` or `OPTIONS` is passed through and answered with the extension's answer.

- The request carries the method, the path below the mount, the raw query string, the headers and the body (up to 1 MiB; larger is refused with 413).
  For a stored request the `context` names the delivery: `invocation_id` and `idempotency_key` are the delivery identity, the same for every attempt, and
  `attempt` counts from 1. The body is exactly as it arrived, so a signature verifies.
- `Cookie`, `Authorization`, `Host` and connection headers are **not** forwarded, so a backend never receives the credentials of Stage's own users.
- The endpoint is anonymous: a webhook's caller is not a Stage user. **The extension must verify the request itself**, for example with the
  signature header its provider sends, and answer `401` when it does not verify.
- For a stored request the answer's `status` decides: `2xx` is delivered, `4xx` other than `408`, `425` and `429` is rejected for good, and anything else, or
  no answer, is retried with a growing wait.
- For a read the answer's `status` must be 100 to 599, or Stage answers 502. `Set-Cookie` and connection headers in the answer are dropped. A backend that is
  down is a 502, and one that does not answer in 30 seconds is a 504.

## Health: `Health`

Answers `STATUS_SERVING` or `STATUS_NOT_SERVING` with an optional `detail`.

## Versioning

The protocol version is `major.minor`, now **1.0**. The package name carries the major (`v1`).

- A **minor** version only adds: fields, messages, enum values, rpcs and services. A peer ignores what it does not know. Both sides must treat an
  unknown enum value as the zero value, "unspecified".
- A **major** version may break. Stage refuses a backend whose major differs at the handshake with an error that names both versions, and an SDK
  refuses a Stage whose major it does not speak.
- The `.proto` files are checked with `buf lint` and `buf breaking` (rule set `WIRE_JSON`), so an accidental break fails the build.
- Field numbers are never reused. A removed field's number and name are marked `reserved`.

Each SDK package carries the version of the protocol it was built from. SDK versions follow semantic versioning on their own: a protocol minor
version that adds an rpc is a minor version of the SDK that exposes it.

## See also

- [Host services reference](host-services.md)
- [Connect a backend to Stage](connect-a-backend.md)
- [Durable ability invocations](../reference/ability-invocations.md)
