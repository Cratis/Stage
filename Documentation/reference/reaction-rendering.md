---
title: Event reaction rendering
description: How Stage renders event-triggered Automation slices, their side effects and their remaining limits.
---

Stage renders event-triggered reactions in Automation slices as Chronicle reactors. The semantic Stage Host and the independent in-memory specification executor also run event reactions.

| Execution path | Event reaction behavior |
| --- | --- |
| Semantic Stage Host | Settles productions, invocations and cascades with the command; appends all facts in one tail-guarded batch or refuses the entire command |
| Rendered Chronicle reactors | Independent, eventually consistent observers; a failed partition can retry after the initiating command has committed |
| In-memory specification executor | Settles event reactions after commands and direct appends with its own deterministic loop; stops at the first rejection or unsupported capability |

The Host applies `where`, `runs as` and `from` through Screenplay's reference settlement. Clock,
application, startup and shutdown triggers and opaque bodies remain unsupported. Host execution does not
relax renderer admission: the diagnostics below describe generated reactors, not the live semantic Host
or the specification executor. The executor also applies `where`, `runs as`, `from` and mixed effects;
invocations use declared system roles or no caller, never the specification's `given caller`. System
claims remain unknown. It enforces append constraints and a limit of 1000 new facts, excluding given
history. Invoked generated commands remain unsupported without deterministic fixtures.

## Model to C# mapping

| Modeled construct | Generated backend |
| --- | --- |
| Automation slice | One slice file containing its commands, validators, events, read models, projections and queries; separate reducer and reactor files |
| Reaction | A class implementing `Cratis.Chronicle.Reactors.IReactor` in the slice namespace |
| Reaction identity | `[Reactor("<semantic id>")]`; renaming the declaration does not select a new observer when its semantic identity is retained |
| Replay policy | Class-level `[OnceOnly]` |
| `runs as system` | Class-level `[ExecuteCommandsAsSystem]`; declared roles become attribute arguments, in canonical order |
| Event triggers | One handler per triggering event type; triggers on that event are combined in authored order |
| `where` | A guard over the triggering event's values |
| `from Source` | `[EventSourceType("<stored source name>")]`; accepts every stream of that source |
| `from Source.Stream` | `[FromEventSource<TSource>("<stored stream name>")]`; accepts only that source and stream, independent of stream identity |
| `invokes` | Returned model-bound command records, executed by Arc's reactor result handlers |
| `produces` | Explicit `EventForEventSourceId` envelopes, never bare event returns |

For example, this reaction excerpt assumes `Started` carries `id` and `amount`, and `CompleteWork` declares those request inputs plus an optional `note`:

```screenplay
reaction CompletePositive
  where amount > 0
  when Started
    invokes CompleteWork
      id = id
      amount = amount
```

The handler adds `new CompleteWork(@event.Id, @event.Amount, null)` only when the amount is positive. Unmapped optional command properties receive `null`; required inputs must be mapped. Generated command properties are not constructor arguments: Arc supplies them when it executes the returned command. The reactor never calls `ICommandPipeline` itself.

A production without `for` targets the triggering event source. An explicit `for` retains its literal or triggering-event property value. Each envelope defaults to Chronicle's unrouted source and stream metadata. Contract tags precede production tags. A `$context.occurred` payload mapping reads `EventContext.Occurred` and sets the envelope's `Occurred` to that same value; productions without that mapping do not override append time.

## Ordering, retry and catch-up

Within a handler, Stage collects effects in authored trigger and mapping order. Arc executes a returned command collection in order and stops at its first failure. Separate reactors are independent observers: do not rely on the order in which their follow-up events arrive.

`runs as system` supplies Arc's system principal only to returned commands. Role-only and authenticated-only authorization gates render. A claim-reading gate, including a claim under `not` or `or`, or an opaque gate under `runs as` fails with `STAGE-ESM-034`: Screenplay treats system claims as unknown, while Arc supplies `[System]` subject claims. Generated policies are unchanged.

A denied, rejected or throwing invoked command fails the live reactor partition and can be retried. Screenplay's reference execution instead ends the scenario at that failure. A reaction without `runs as` can render while invoking an authorization-gated command, but the live invocation has no principal and can leave a failed partition. Rendering is not proof of successful authorization or settlement.

A newly registered reactor catches up from the existing event log. `[OnceOnly]` prevents replay; it does **not** deduplicate ordinary deliveries or retries after failure. Keep commands and external effects safe for repeated delivery, and inspect failed partitions during operation.

## Specifications and verification

The in-memory executor runs event-reaction specifications without building a generated application.
Given events establish history without firing reactions. Direct-append then-events describe only the
reaction effects; command then-events include the initiating command's facts and all cascading effects.
An authored `given clock` supplies `$context.occurred` for command, direct-append and named-redelivery
specifications, including cascading productions, invocation arguments, invoked commands and refusal
productions. Offset-bearing source clocks normalize to canonical UTC; the instant must be at or after the
Unix epoch and millisecond-aligned. Mapped values use UTC round-trip text, for example
`2027-03-04T08:00:00.1230000Z`. Redelivery uses this scenario clock, not a timestamp from given history.

No authored clock means no reaction occurrence: the executor never substitutes its injectable clock or
wall time. A reached production mapping rejects with `Reaction '<name>' uses $context, which needs the
scenario's 'given clock'.`; an invocation argument using the missing occurrence instead rejects with
`An execution expression is unresolved in its declared root scope.` A false `where` skips evaluation.
Refusal branches reuse the same occurrence; the first matching branch wins, and a failed branch production
is not offered to a later branch. The clock supplies time only: `runs as system` does not manufacture audit
identity, and `$eventSourceId` (also spelled `$eventContext.eventSourceId`) and `$context.causedBy.*`
retain `STAGE-ESM-013`. `$context.eventSourceId` is not a valid reaction mapping.
Clock/trigger/capture actions and scheduling triggers remain unsupported.

Stage renders single-hop `when append` reaction specifications through `ReactorScenario`. Givens establish history and are not delivered to the reactor. A production-only scenario checks the exact effect count, payloads, destinations and all three unrouted metadata values (`EventSourceType.Default`, `EventStreamType.All`, `EventStreamId.Default`), with assignment matching for any-order expectations. An invocation scenario executes the one returned command through `CommandScenario`, reading the system identity from the reactor's attribute rather than forwarding `given caller`. Screenplay requires an outcome: a `when append` specification always asserts the reaction's produced events or the invoked command's outcome.

Append fixtures may carry an explicit source and route. A sourceless append uses a fixed scenario placeholder only when no production relies on the trigger destination. Fan-out, deeper cascades, multiple invocations, `$context`, read-model/query expectations and constraint-target appends or products remain refused with `STAGE-ESM-011`. Keyed read-model absence expectations have the earlier `STAGE-ESM-027` refusal. Non-reaction appends also remain refused. The append values must select exactly one active trigger after `from` and `where` are evaluated; an excluded append fails admission with `STAGE-ESM-011`. Guarded alternative paths can each be specified with their own outcome.

The generated `CommandScenario` does not settle reactions, whereas the reference does. When a command specification produces exactly one event that fires exactly one invoke-only reaction, Stage splits it: an accepted specification becomes a command half that asserts the triggering events and a `ReactorScenario` half that appends the triggering event and asserts the rest; a rejected specification becomes only the reaction half, and only when the triggering command cannot reject on its own (no validations, requirements, decision reads, routes, authorization, generated values, constrained or constraint-releasing products, rule-carrying concepts, and matching input values). Every other command specification whose command produces an event subscribed to by an event reaction stays refused with `STAGE-ESM-011`. The split is not atomic: the reference rejects the whole command when the invoked command rejects, while a rendered application commits the triggering command and its reactor fails afterwards.

Generated backend `$context.occurred` mappings remain supported through `EventContext.Occurred`, but
context-dependent generated reaction specifications retain `STAGE-ESM-011`. Authored clocks on generated
append/redelivery specifications additionally retain `STAGE-ESM-024`; the renderer's clock fixture support
remains command-only. Independent executor support does not broaden that generated scenario contract.

Render admission, generated Debug compilation, generated specifications and live execution are separate results. Build and test the generated project rather than treating a published render as runtime evidence. Live verification requires a Chronicle kernel and the generated Arc host wiring, including its reactor result handlers.

## Limits and diagnostics

| Diagnostic | Refused shape |
| --- | --- |
| `STAGE-ESM-024` | Clock, application, startup and shutdown triggers; opaque reaction bodies; captures and Translate slices; clock/trigger/capture actions; generated append/redelivery specifications with clock fixtures |
| `STAGE-ESM-034` | `runs as` invoking a claim-reading or opaque authorization gate; all `runs as` on legacy syntax paths |
| `STAGE-ESM-035` | Reaction `from` on legacy syntax paths only; use the semantic planner |
| `STAGE-ESM-030` | Reserved stored source or stream names in observer filters |
| `STAGE-ESM-037` | Events and commands returned by one handler, whether authored in one trigger or separate triggers on the same event in one reaction |
| `STAGE-ESM-013` | Reaction mappings of `$context.causedBy.*`, `$eventSourceId` or `$eventContext.eventSourceId` |
| `STAGE-ESM-039` | Conditions, destinations or mappings that cannot retain their modeled meaning; incomplete event mappings or a reaction without an event subscription |
| `STAGE-ESM-012` | Reactor names or handler names that collide with generated C# declarations or slice files |
| `STAGE-ESM-011` | Append specifications outside the single-hop subset above; command specifications that cascade into reactions |
| `STAGE-ESM-007` | A query or projection in an Automation slice without a locally declared read model and its single projection or reducer |
| `STAGE-ESM-009` | A flat projection whose payload key cannot be proven equal to the append destination for every command and reaction producer |
| `STAGE-ESM-040` | Personal or secret concepts used as reaction production destinations |

Conditions cannot use optional, composite, collection or enum-valued operands; ordering requires numbers. Property mappings must retain the exact source and target type. Commands, projections, reducers and queries declared in an Automation slice retain their existing admission limits. Query-only and projection-only surfaces are checked like StateView slices, not silently omitted. For a flat projection, every reaction production of its observed event must explicitly target the same value mapped into the payload key. An omitted `for` uses the triggering event source and does not prove that equality. This check includes producers outside the selected render scope. Personal and secret concepts cannot supply append identities; use a surrogate destination and keep the protected value in the payload.

Stage's pinned Arc release supplies `EventsAndCommandsResultHandler`, which can return events and commands from one handler. Stage still refuses that shape with `STAGE-ESM-037`: the handler appends the events before it runs the commands, so a refused command fails the partition, and Chronicle's redelivery appends the same events again. Screenplay's reference and the Stage Host settle a reaction all or nothing. Splitting the effects into two reactors would avoid the duplicates but still commit the events when the command is refused. Stage lifts 037 once reactor appends are idempotent across redelivery ([Chronicle#4809](https://github.com/Cratis/Chronicle/issues/4809), [Arc#3207](https://github.com/Cratis/Arc/issues/3207)). Per-production route overrides retain `STAGE-ESM-036`; Screenplay already refuses reaction production routes during binding.

Scoped rendering may reference an invoked command or triggering event in an unselected slice, as it does for cross-slice event references. Include those dependencies when compiling the application.

See [Build a renderer target](../guides/build-renderer-target.md) for the full admission table and [Customize a rendered application](../guides/customize-rendered-application.md) for unmanaged extensions.
