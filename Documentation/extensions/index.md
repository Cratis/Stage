---
title: Extension backends
description: How a Cratis Screenplay system reaches something outside the model - an extension backend in any language, called by Cratis Stage over a two-way gRPC protocol, with host services for settings, secrets, triggers, answers and telemetry.
---

A Screenplay `system` declares what the business can ask an outside system to do: post to a chat channel, book a pickup, take a payment. The
model names the abilities; an **extension backend** is the code that realizes them. Stage hosts backends **outside its own process** and talks
to them over a two-way gRPC protocol, so a backend can be written in any language and needs as little infrastructure of its own as possible:
it asks Stage for its settings, its secrets and its telemetry instead of building them.

This section is the reference for that protocol, the services Stage offers, and the SDKs that implement the extension side for C#, Kotlin, Go
and TypeScript. It implements section 9 of
[decision 0159](https://github.com/Cratis/Screenplay/blob/main/decisions/0159-systems-with-abilities-extensions-and-durable-invocations.md).

## How the pieces fit

```text
 .play model                Stage (this repository)                          Extension backend
 -----------                ------------------------------                   -----------------
 system Slack               durable invocation (Choreography)                 your code, any language
   ability Post   ───────►  IAbilityBackend ──── ExtensionBackend (gRPC) ──►  describe / invoke /
   trigger Replied                  ▲                                          handle HTTP / health
                                    │
                            StageHost (gRPC) ◄──────────────────────────────  settings, secrets, raise
                            host services                                      trigger, answer later,
                                                                               logs and metrics
```

- Screenplay defines the contract: the `system`, its abilities, their effect classes and outcomes. Read the
  [systems and abilities](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/systems.md) page.
- Stage runs each ability invocation as a durable, retried, deduplicated message. See
  [Durable ability invocations](../reference/ability-invocations.md) and the Choreography page on
  [answers that arrive later and reconciliation](https://github.com/Cratis/Choreography/blob/main/Documentation/workflows/extension-invocations.md).
- Inbound traffic, such as a webhook, is stored and acknowledged by Stage, then delivered to the extension, which raises the contract's triggers. See
  [inbound traffic and webhooks](inbound-traffic.md) and the Screenplay
  [triggers](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/triggers.md) page.
- The extension protocol is the seam between the two: for every attempt Stage calls the backend, and the backend answers with what happened.

- A model reaches a running extension through the catalogs Stage builds from the compiled model and the locked packages. See
  [From a model to a running extension](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/end-to-end.md).

## Where things live

Decision, recorded here because it was left open by decision 0159: **the protocol and the SDKs live in the Cratis/Stage repository**, under
`Source/Extensions`.

| What | Where |
| --- | --- |
| The protocol, as versioned `.proto` files | [`Source/Extensions/Protocol`](https://github.com/Cratis/Stage/tree/main/Source/Extensions/Protocol) |
| C# SDK (`Cratis.Stage.Extensions`) | [`Source/Extensions/DotNET`](https://github.com/Cratis/Stage/tree/main/Source/Extensions/DotNET) |
| Kotlin SDK (`io.cratis.stage:stage-extensions`) | [`Source/Extensions/Kotlin`](https://github.com/Cratis/Stage/tree/main/Source/Extensions/Kotlin) |
| Go SDK (`github.com/Cratis/Stage/Source/Extensions/Go`) | [`Source/Extensions/Go`](https://github.com/Cratis/Stage/tree/main/Source/Extensions/Go) |
| TypeScript SDK (`@cratis/stage-extensions`) | [`Source/Extensions/TypeScript`](https://github.com/Cratis/Stage/tree/main/Source/Extensions/TypeScript) |
| The conformance suite every SDK passes | [`Source/Extensions/Conformance`](https://github.com/Cratis/Stage/tree/main/Source/Extensions/Conformance) |

Why one repository: the protocol and the host that implements it change together, an SDK is only correct against the protocol it was built from,
and one conformance suite is easiest to keep honest when every SDK sits beside it. Splitting an SDK out later is a move, not a redesign, because
each one depends on the `.proto` files alone.

## What version one includes

Included: every direction the protocol needs to realize abilities durably.

| Direction | Service | Included |
| --- | --- | --- |
| Stage to extension | `ExtensionBackend` | `Describe` (handshake against the contract), `Invoke` (one attempt), `HandleHttp` (routed inbound HTTP), `Health` |
| Extension to Stage | `StageHost` | `GetSetting`, `GetSecret`, `RaiseTrigger`, `Answer`, `Log`, `RecordMetrics` |

Every request carries the tenant, the invocation identity, the idempotency key, the attempt, the deadline and the trace context.

**Left out of version one, deliberately:**

- **A key-value state store scoped to system and tenant.** An extension that needs to remember something between calls has the idempotency
  key and the invocation identity Stage already keeps durably, and can use its own store. A host-owned store would make Stage responsible for
  another persistence surface with its own tenant isolation, size limits, retention and migration story, and it cannot be removed once extensions
  depend on it. Adding a service to the protocol is a backward-compatible minor version, so it can follow when a real extension needs it.
- **An outbound credential proxy.** The point is that a secret never enters the extension. That is a real security gain, but it needs a
  declarative way to say which hosts an extension may reach and how a credential is attached, and that belongs in the contract the language
  defines (Cratis/Screenplay#1254), not in the transport. Until then secrets reach the extension only through `GetSecret`, held in memory,
  never in an environment variable or a file Stage writes.

Both are reserved: a future minor version adds them as new services without changing the ones above.

## Next

- [Protocol reference](protocol.md): messages, rules and compatibility.
- [Host services reference](host-services.md): what the extension can ask of Stage, and how each answer should be handled.
- [Connect a backend to Stage](connect-a-backend.md): configuration, the handshake and what Stage checks.
- [Built-in `http` extension](http.md): realize an ability as a REST call from a declarative block, with no backend to run.
- [Extension packages](packages.md): how Stage loads the packages a model's lock pins, decides whether it may use them, and serves what they ship.
- [Execution tiers and activation](execution-tiers.md): how a backend runs, when it starts and stops, and who may override that.
- [Conformance suite](conformance.md): what every SDK must do, and how to add a language.
- Write a backend: [C#](csharp.md), [Kotlin](kotlin.md), [Go](go.md) or [TypeScript](typescript.md).

The frontend side of an extension, the components it ships for screens, is separate. See the Scene
[blueprint components](https://github.com/Cratis/Scene/blob/main/Documentation/blueprint-components/index.md) for how screens are composed, and
Cratis/Stage#1070 for how Stage hosts them.
