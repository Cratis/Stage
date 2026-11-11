---
title: Durable ability invocations
description: How Cratis Stage runs Screenplay ability invocations as persisted messages on its Orleans silo, how it names, retries, gates and reconciles them, and how to list and settle the ones that stopped.
---

An ability is something a Screenplay `system` can be asked to do: post to a chat channel, book a pickup, take a card payment. Cratis
Stage runs every ability invocation as a persisted message on the Orleans silo it already hosts, through
[Cratis Choreography](https://github.com/Cratis/Choreography). This page describes what the runtime guarantees, how an invocation is
named, what each effect class does when an attempt may have reached the system, how stopped invocations are reconciled, and how a
deployment limits the calls to a system.

Screenplay defines what an invocation means. Read the Screenplay pages on
[reactions](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/reactions.md),
[workflows](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/workflows.md),
[state machines](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/statemachines.md) and
[operations](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/operations.md) for the model side, and
[decision 0159](https://github.com/Cratis/Screenplay/blob/main/decisions/0159-systems-with-abilities-extensions-and-durable-invocations.md)
for the contract this page implements. The engine side is described in
[Answers that arrive later, capability gates and reconciliation](https://github.com/Cratis/Choreography/blob/main/Documentation/workflows/extension-invocations.md)
and in the [Choreography runtime](https://github.com/Cratis/Choreography/blob/main/Documentation/workflows/runtime.md).

## What is available today

The runtime described here ships in Stage. The Screenplay syntax that declares abilities on a `system` and invokes them with
`invokes <System>.<Ability>` is tracked in [Cratis/Screenplay#1250](https://github.com/Cratis/Screenplay/issues/1250) and is not part of
the Screenplay release Stage builds against, so no model can produce an invocation yet. Stage opens the runtime through three seams, and
that syntax and the extension protocol plug into them without changing the runtime:

| Seam | What fills it |
| --- | --- |
| `IAbilityCatalog` | One `AbilityDescriptor` per ability a model's systems declare: the ability and its contract major, its effect class, the outcomes it can end in, the locked package digest, and its attempt policy. |
| `IAbilityBackend` | The extension protocol. The runtime hands it an `AbilityCall` and gets an `AbilityResult` back. With `Stage:Extensions:HostPort` set, `GrpcAbilityBackend` calls the backend over the [extension protocol](../extensions/protocol.md); otherwise `NoAbilityBackend` answers that nothing was sent. |
| `IInvocationOriginHandler` | One handler per origin kind, which takes the outcome back to the behavior that asked for it. |

Until a catalog is registered, the runtime has no abilities and refuses every request to invoke one.

## How an invocation runs

```text
reaction branch ─┐   workflow action ─┐   state machine entry ─┐
                 ▼                    ▼                        ▼
   IAbilityInvocations.Invoke: the invocation is recorded with its cause (the outbox)
                                      │
                                      ▼
        invocation run: a workflow run keyed by the invocation identity,
        capability  ability:<System>.<Ability>@<contract major>, package digest pinned
                                      │
                                      ▼
        gates: per-system concurrency and rate limits from deployment configuration
                                      │
                                      ▼
        the ledger notes that the attempt began, then IAbilityBackend.Invoke
                                      │
                                      ▼
        settle node: records the outcome and calls the origin's handler
```

1. **The outbox comes first.** `Invoke` writes the invocation to the ledger, with the origin and the causation that produced it, before any run
   exists. An invocation is never held only in memory.
1. **One run per invocation.** The run is the workflow named after the capability id, started with the invocation identity as its
   deduplication key. Its `invoke` node performs the ability under the capability `ability:<System>.<Ability>@<contract major>`; every outlet the
   ability can end in (`returned`, each declared outcome, `failed`, and `expired` for an ability that answers later) leads to a `settle` node.
1. **The run pins the package.** The run records the locked package digest the ability had when the invocation started, so upgrading an extension
   never changes an invocation in flight. A capability asked for at a contract major the model does not declare has no performer, and the engine
   fails the node instead of calling an extension built for another contract.
1. **Settling is the last step, whichever way the ability ended.** It returns, an answer arrives, an operator reconciles it, or its attempts run
   out: the `settle` node records the outcome and hands it to the origin. Delivery is at least once, so a handler finds what it already did by the
   invocation identity.

Invoking is safe to repeat. A request whose identity is already recorded returns the existing invocation, and the engine finds the run that
exists for the deduplication key instead of starting another.

## Invocation identity

The identity of an invocation is deterministic. It is derived from the origin's semantic address plus its causation, and never includes the attempt:

| Part | Example |
| --- | --- |
| Origin kind | `ReactionBranch`, `WorkflowAction`, `StateMachineEntry` |
| Origin address | The invoking use in the model, such as `Fulfillment/PostToWarehouse/branch 2`. It does not change between occurrences. |
| Causation | What caused this occurrence: the triggering event's sequence number, the command's correlation, the transition's event, or the run's node execution. |
| Ability | The capability id, without the contract major, so an extension upgrade does not make a redelivery look new. |

The identity is a fixed-length digest (`inv-` and 32 hexadecimal characters). One value does three jobs:

- It is the **grain key** of the invocation, so redelivery of a reaction or a state machine entry finds the invocation that exists.
- It is the **deduplication key** of the invocation's run.
- It is the **idempotency key** the extension receives in `AbilityCall.IdempotencyKey` (`$invocation.idempotencyKey` in the model). It is the same
  for every attempt, which is what lets an extension deduplicate the effect.

A workflow action that performs an ability directly has no separate run: the action node is the invocation, and its identity is derived from the
run and the node execution.

## Effect classes

Every ability declares an effect class. The class decides what the runtime does when an attempt fails, and when an attempt may have reached the
system without the runtime learning how it ended: an exception, a timeout, or a restart in the middle of the call.

| Class | Meaning | When an attempt may have reached the system |
| --- | --- | --- |
| `safe` | No lasting effect. | The attempt fails and is retried freely. |
| `idempotent` | The system deduplicates the effect by the idempotency key. | The attempt fails and is retried with the same key. |
| `once` | The effect must happen at most once. | The attempt is **never retried**. The invocation stops in `reconciliation`, and an operator records which outcome happened. |

How an attempt ends decides which of those paths it takes:

| The backend answers | Result |
| --- | --- |
| `AbilityReturned` | The invocation settles through `returned` or the named outcome. An outcome the ability does not declare is treated as an attempt that may have reached the system. |
| `AbilityAccepted` | The system accepted the work and will answer later; see [Answers that arrive later](#answers-that-arrive-later). |
| `AbilityNotSent` | The call certainly did not reach the system. The attempt fails and is retried, **even for `once`**, because nothing could have happened. |
| `AbilityFailed` with `MayHaveReachedTheSystem` false | The system did nothing. Retried like `NotSent`. |
| `AbilityFailed` otherwise, or an exception, or a timeout | The effect may have happened. `safe` and `idempotent` retry; `once` stops. |

When the attempts of a `safe` or `idempotent` invocation run out, the invocation fails and the origin takes the `failed` branch.

### A restart in the middle of an attempt

Choreography can run a performer twice for one attempt: a recovered wave job performs the attempt again after a restart. Before it calls the system,
the performer asks the invocation's ledger to note that the attempt began.

- For a `safe` or `idempotent` ability the attempt simply runs again, with the same idempotency key.
- For a `once` ability, the ledger refuses an attempt that finds an earlier one begun and without an outcome. The performer answers that the outcome
  is ambiguous, Choreography never retries that node, and the invocation stops in `reconciliation` with the reason recorded.

A `once` effect is therefore never repeated by a restart, provided the ledger survives the restart. See [Durability of the ledger](#durability-of-the-ledger).

## Origins

An origin handler takes a settled invocation back to the behavior that asked for it. Register one `IInvocationOriginHandler` per
`InvocationOriginKind`. `InvocationBranch.Of(record)` names the branch the origin takes: `returned`, the name of the outcome, or `failed`.

| Origin | What the outcome does |
| --- | --- |
| Reaction branch | The branch settles: `on returned`, `on <Outcome>` or `on failed`, together with the rest of its atomic batch. |
| Workflow action | The workflow takes the outlet the outcome names. |
| State machine entry | The entry effect completes. A failure stops the entry for an operator. |

A settled invocation whose origin kind has no handler fails its `settle` node, which the engine retries; it is not silently dropped.

Pre-commit operations are not durable messages. They run inside the command request through Arc's command operations, their failure fails the
command, and their compensation follows the commit disposition. Post-commit effects belong in reactions.

## Answers that arrive later

An ability that answers later returns `AbilityAccepted`. The invocation becomes `Suspended` and its node waits like a `wait` node, without holding a
worker or a timer, under a completion key that is the invocation identity. The wait ends when the invocation is answered or expires.

```csharp
var result = await invocations.Answer(identity, "approved", """{"note":"ok"}""", principal);
```

`Answer` returns an `InvocationAnswer`:

| Result | Meaning |
| --- | --- |
| `Answered` | The answer was taken, and the origin continues through the outlet it names. |
| `Forbidden` | The identity is not authenticated, or does not satisfy the policy the invocation names. |
| `UnknownOutlet` | The outlet is not `returned` or an outcome the ability declares. |
| `NotWaiting` | The invocation is not suspended, including when it was already answered. |
| `Unknown` | No such invocation was recorded. |

The identity that answers is checked first, by the invocation's `AnswerPolicy` against the host's authorization (any authenticated identity when no
policy is named). Then the identity is recorded with the invocation (`InvocationRecord.DecidedBy`) before the run takes the answer, so the record
says who decided whichever node ends the invocation. This is how an `approval ... via` records its answering identity.

An answer that does not arrive within `AnswerExpiresAfter` takes the `expired` outlet. The invocation ends as `failed` with the reason that no answer
arrived, and the origin takes the `failed` branch. A zero expiry never expires.

## Reconciliation

An invocation whose effect is `once` stops when an attempt may have reached the system. Its status is `Stopped`, its run ends as
`ReconciliationRequired`, and the reason says what is unknown. Nothing retries it.

An operator finds out what happened, asking the external system what it did for the invocation identity, and records it:

```text
GET  /stage/invocations?status=stopped
POST /stage/invocations/{identity}/reconciliation
```

```json
{ "outcome": "Declined", "output": "{\"reason\":\"insufficient funds\"}", "actor": "alice" }
```

The list gives each invocation's identity, capability, effect class, status, origin and causation, attempts, outcome, reason, who decided, run,
package digest and times. `outcome` is `returned`, an outcome the ability declares, or `failed`. Stage records the choice and who made it, resumes
the run from it, and the settle node hands the chosen outcome to the origin: the reaction branch, workflow outlet or state machine entry continues
from that choice. The extension is not called again.

| Response | Meaning |
| --- | --- |
| 404 | No such invocation. |
| 400 | The outcome is not one the ability declares. |
| 409 | The invocation is not stopped, including when it was already reconciled. Reconciling is safe to repeat. |
| 202 | The outcome was recorded and the origin continues from it. |

Answering a suspended invocation uses `POST /stage/invocations/{identity}/answer` with `{ "outlet": "approved", "payload": "{}" }` and the
status codes `202`, `404`, `403`, `400` and `409` for the results above.

These endpoints are tooling in the sandbox. A published runtime does not map them, like the other `/stage/` tooling routes.

## System gates

A slow or failing system fills its own gate and nothing else. Deployment configuration limits each system:

```json
{
  "Stage": {
    "Invocations": {
      "Systems": {
        "Slack": { "MaxConcurrent": 4, "RequestsPerSecond": 1, "MaxAttempts": 2 },
        "Billing": { "MaxConcurrent": 1 }
      }
    }
  }
}
```

| Key | Effect |
| --- | --- |
| `MaxConcurrent` | How many calls to the system may be in flight at once. Stage maps it onto the engine's capability gate `ability:<System>.*`, shared by every ability of the system and every contract major. A call waits for room inside the attempt's timeout, and if none comes the attempt times out without the extension being called. |
| `RequestsPerSecond` | How many calls per second may be made. A call beyond the rate waits. |
| `MaxAttempts` | Narrows the attempts every ability of the system may use below what the ability declares. |

Both limits apply per silo, and neither crosses silos: divide a system's allowance by the number of silos. The attempts, timeout and retry delay an
ability uses otherwise come from the ability's own policy.

## Durability of the ledger

The ledger is a grain per invocation (keyed by the invocation identity) and an index grain that lists them for the Workbench, persisted through the
workflow storage provider of the silo. Choreography journals the runs in Chronicle, but the ledger lives in Orleans grain storage, and what it
guarantees depends on that storage:

| `Stage:Orleans:Storage` | Ledger survives a process restart |
| --- | --- |
| `InMemory` (the default) | No |
| `MongoDB` (with `Stage:Orleans:ConnectionString` and `Stage:Orleans:DatabaseName`) | Yes |

The guard that stops a `once` effect from repeating is a record that an attempt began, so it protects against a restart only when the record
survives it. Stage therefore **refuses a `once` ability when the ledger is in memory**: the request to invoke it is not admitted, with a reason that
names the setting, and a workflow action that performs one fails its attempt before calling the system. `safe` and `idempotent` abilities are not
affected, because repeating them is harmless.

For a sandbox, where nothing outside the session can be hurt, set `Stage:Invocations:AllowVolatileOnce` to `true` to run `once` abilities against the
in-memory ledger and accept that a restart can repeat the effect.

## Command actions report a refusal apart from a failure

A workflow action that invokes a command distinguishes two outcomes, as in the
[Screenplay workflow reference](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/workflows.md):

| Outlet | Meaning |
| --- | --- |
| `failed` | The invocation itself broke: the command could not run, or did not settle. The engine retries it up to `attempts`. |
| `refused` | The command was handed on and something rejected it: validation, authorization or a constraint. It is not a failure of the invocation. |

When the model connects the `refused` outlet, a refusal leaves through it. When it does not, the refusal fails the node, as an unconnected
`refused` outlet fails the run in Screenplay's reference evaluation. The sandbox and the generated action performers behave the same way. See
[State machine and workflow execution](state-machines-and-workflows.md).
