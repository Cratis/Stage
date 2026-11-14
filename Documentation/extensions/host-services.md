---
title: Host services reference
description: "What a Cratis Stage extension backend can ask of Stage: settings, secrets, raising triggers, late answers, and logs and metrics tied to the invocation."
---

Stage offers extensions the services below through `service StageHost`, defined in
[`host.proto`](https://github.com/Cratis/Stage/blob/main/Source/Extensions/Protocol/cratis/stage/extensions/v1/host.proto). An extension needs
no infrastructure of its own for any of them.

## Calling the host services

- Stage listens on its own gRPC port, separate from the application's. It is off until `Stage:Extensions:HostPort` is set. See
  [Connect a backend to Stage](connect-a-backend.md).
- Every call carries `authorization: Bearer <session_token>` metadata. The token comes from the `Describe` handshake, is scoped to one system, and
  lives as long as the Stage process.
- The **system always comes from the token**, never from the request. An extension cannot read another system's settings, answer another
  system's invocations or raise another system's triggers by naming them.
- Call the host services only **after `Describe` has returned**. A call before that is refused with `FAILED_PRECONDITION`, because Stage learns
  what the extension declared from the handshake.
- A call without a token, or with one Stage did not issue, is refused with `UNAUTHENTICATED`.
- Pass the `RequestContext` of the invocation you are working on, so what you report is tied to it.

## Settings and secrets

| Rpc | Returns |
| --- | --- |
| `GetSetting(name)` | A **public** setting: `FOUND` with the value, `NOT_SET`, `UNKNOWN` (not declared by the extension), or `IS_SECRET` (declared as a secret; ask `GetSecret`). |
| `GetSecret(name)` | A **secret**: `FOUND` with the value, `NOT_SET`, `UNKNOWN`, or `NOT_SECRET` (declared as a public setting; ask `GetSetting`). |

The two are separate on purpose. A value declared `secret` is **never** returned by `GetSetting`, so a public setting that can reach a browser and
a secret that never does cannot be mixed up by a typo.

Where Stage reads values from: configuration, under `systems:<System>:<name>`, which is the environment variable `systems__<System>__<name>` on
the Stage process or any other configuration provider, such as a secret store mounted by the deployment. This matches the default of
[decision 0159](https://github.com/Cratis/Screenplay/blob/main/decisions/0159-systems-with-abilities-extensions-and-durable-invocations.md): a
setting is `systems.<System>.<setting>` unless the model names a secret explicitly. The model-side naming (`secret <Name>`, `setting <name> from
secret <Name>`) is language work tracked in [Cratis/Screenplay#1254](https://github.com/Cratis/Screenplay/issues/1254); the lookup is behind
`ISystemSettings`, so it plugs in without changing the protocol.

How secrets are kept:

- A secret reaches the extension **only** through `GetSecret`. Stage never puts one in the extension's environment or in a file.
- Stage never logs a secret value.
- The extension should keep a secret in memory only, never log it, and never put it in an ability's output, which Stage persists.

## Raise a trigger

`RaiseTrigger(trigger, deduplication_key, payload_json, correlation_key)` raises a trigger the extension's contract declares, so a model that reacts to it
starts and a workflow that waits for it resumes. A webhook handler or a poll uses it; see [inbound traffic and webhooks](inbound-traffic.md).

| Status | Meaning |
| --- | --- |
| `RAISED` | Every reaction was started, or already had been for this key, or a run that waits for the trigger resumed. |
| `UNKNOWN_TRIGGER` | The extension did not declare this trigger at the handshake. |
| `NOT_ROUTED` | Nothing in the model reacts to the trigger and no run waiting for it resumed. It is reported, never silently accepted, because a raise that does nothing is a bug worth seeing. |
| `REFUSED` | The deduplication key is missing, or the runtime did not admit a reaction. `detail` says which. |

The **deduplication key is required**. Raising the same trigger with the same key again finds the runs the first raise started instead of
starting more, so a retry, or a webhook the provider delivers twice, is harmless. Inside an invocation use its idempotency key; for a webhook use the
external message's own identity.

The optional **correlation key** says what the trigger is about, such as the payment a webhook reports on. Runs that wait for `<System>.<Trigger>` about the same
correlation resume, with the payload as the wait's data. The reactions run as the actor `extension:<System>`, never as the anonymous sender of a webhook.

Stage starts each reacting workflow as a Choreography `Signal` trigger, deduplicated on the trigger, the key and the workflow. Which workflows
react to which trigger is supplied by the language support for reactions (`IContractTriggerRoutes`); until a model declares one, the answer is
`NOT_ROUTED`.

## Answer later

An ability that returns `Accepted` completes later with `Answer(context.invocation_id, outlet, payload_json)`. The outlet is `returned` or one
of the outcomes the ability declares.

| Status | Meaning |
| --- | --- |
| `ANSWERED` | The answer was taken. Answering twice is harmless. |
| `UNKNOWN_INVOCATION` | No invocation has this identity. |
| `NOT_WAITING` | The invocation is not waiting for an answer: it already finished or has not been accepted yet. |
| `FORBIDDEN` | The invocation belongs to another system. |
| `UNKNOWN_OUTLET` | The ability does not declare this outlet. |

A backend answers **as the system it serves**. It may answer only the invocations of its own system, and the persona or policy meant for people
who answer (`AnswerPolicy`) does not apply to it. Who answered is recorded with the invocation.

The identity is the key: keep `invocation_id` so a later event, such as a callback arriving over HTTP, can complete the invocation without Stage
holding a worker meanwhile.

## Logs and metrics

`Log(records)` and `RecordMetrics(metrics)` are tied to the invocation named in the context.

- **Logs** are written to Stage's logging under the category `Cratis.Stage.Extensions.<System>`, with a scope holding `system`, `tenant`,
  `invocation`, `attempt`, `origin` and `causation`, and each attribute as `attr.<key>`. They go wherever Stage's logs go.
- **Metrics** are recorded on the meter `Cratis.Stage.Extensions` as the instrument `stage.extension.<name>`, a counter or a histogram, tagged
  with `stage.extension.system`, `stage.tenant` and the attributes. They are exported wherever Stage's OpenTelemetry metrics go. A system may record
  64 distinct metrics; the rest are ignored, so a bug cannot flood the host. A metric of an unspecified kind is ignored.
- **Traces.** Stage starts an activity `extension.invoke` on the `Cratis.Stage.Extensions` source for each attempt and sends its W3C trace
  context in `RequestContext.traceparent`. An extension that uses OpenTelemetry continues that trace by using the context as its parent, and exports its
  own spans itself. The protocol carries no span data in version one.

The system on every log and metric comes from the token. Attributes are free-form: put no secret and no personal data in them or in a message.

## See also

- [Protocol reference](protocol.md)
- [Connect a backend to Stage](connect-a-backend.md)
- [Inbound traffic and webhooks](inbound-traffic.md)
- [Screenplay triggers](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/triggers.md) and
  [reactions](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/reactions.md), the model side of raising a trigger
