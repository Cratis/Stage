---
title: Event reaction rendering
description: How Stage renders event-triggered Automation slices, their side effects and their remaining limits.
---

Stage renders event-triggered reactions in Automation slices as Chronicle reactors. It also renders a narrow application-trigger subset as Chronicle occurrence handlers. The semantic Stage Host and the independent in-memory specification executor also run event reactions.

| Execution path | Event reaction behavior |
| --- | --- |
| Semantic Stage Host | Settles productions, invocations and cascades with the command; appends all facts in one tail-guarded batch or refuses the entire command |
| Rendered Chronicle reactors | Independent, eventually consistent observers; a failed partition can retry after the initiating command has committed |
| Rendered application-trigger handlers | Discover a `[Trigger]` payload record and `IOccurrenceHandler<T>`; commit direct productions with one receipt and invoke commands with separate per-command receipts |
| In-memory specification executor | Settles event reactions after commands and direct appends, fires application, startup and shutdown triggers (`when trigger`) and due clock occurrences (`when clock`) with its own deterministic loop; stops at the first rejection or unsupported capability |

An application-trigger reaction renders only when it has one named trigger, one handler for that trigger,
and at least one direct `produces` with an explicit `for` or one `invokes`. Mappings use trigger properties or literals.
The payload must contain no personal or protected values, including inside composites or collections.
The handler selects the recording namespace, derives a stable effect key from the reaction's semantic id,
and enrolls that key with `UseEffectReceipt` before one `AppendMany` unit-of-work commit. Receipt and
productions commit together; retrying that occurrence in that namespace does not append again. This is
not exactly-once delivery and is not a server-startup hook. Event-triggered reactors are unchanged.
Application code records the payload with `IOccurrences.Record(payload, idempotencyId)`; Stage renders no
public trigger endpoint. Missing kernel receipt support fails with `EffectReceiptsNotSupported`, not an
unreceipted fallback. External side effects still require their own idempotency.
Invocations run after the productions commit, in modeled order, through Arc's `IOccurrenceCommandExecutor`.
Each command has its own atomic effect receipt, separate from the productions receipt. A receipted
production batch is skipped on redelivery without skipping the invocations; each invocation resumes
through its own receipt. Effect names include the reaction id, invocation index and command id:
reordering invocations changes their keys and can run those commands again.

Commands always run as system, with the roles declared by `runs as system`. Without `runs as`, only
commands without authorization gates render; explicit and inherited authorization otherwise receive
`STAGE-ESM-024`. Claim-reading or opaque gates under `runs as` retain `STAGE-ESM-034`.
Terminal business refusals are recorded. With no `on refused` branch, the handler stops later invocations
and throws, including on redelivery of the stored refusal: there is no implicit acknowledgement.
Authorization failures and exceptions leave no receipt and can be retried. Kernel `effect-receipts`
support is required for both production and command effects.

Application-trigger `on refused`, `where`, `from`, event-context mappings, implicit destinations and
production-only `runs as` remain refused with a narrowed `STAGE-ESM-024`. Startup/shutdown triggers still
need an activation identity and namespace policy; interval/schedule triggers still need Chronicle
schedule and due-time parity.

The Host applies `where`, `runs as` and `from` through Screenplay's reference settlement. Clock,
application, startup and shutdown triggers and opaque bodies remain unsupported in the Host. The renderer's
occurrence subset does not change that boundary or the executor's broader support. Host execution does not
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

## Authored C# bodies (native execution; modeled execution unsupported)

The semantic renderer accepts an authenticated inline `csharp` block or resolved `.cs` file on a reaction with exactly one event trigger. Its synchronous handler returns `IEnumerable<object>?` and receives the typed triggering event as `@event` and Chronicle's `EventContext` as `context`. Each event property also has a PascalCase local binding, such as `var Title = @event.Title;`. A body may return `null`, as on the existing legacy statement-body path; a missing implementation is always refused, never replaced with a successful no-op.

Inline and file C# bodies returning both events and commands are refused with `STAGE-ESM-037`: Arc appends events before executing commands, so a failed command leaves those events appended; use two reactions, or produce the event and let a second reaction invoke the command. Stage conservatively inspects handler `return` and `yield return` syntax, recognizing modeled events, envelopes, commands, arrays, collection initializers, branches and unmodified locals with modeled declared or inferable types. Undeterminable shapes (including helper-call results, `object`/`dynamic` locals, mutated collections, casts and conditional-compilation directives) fail closed with the same diagnostic. Inspectable events-only and commands-only bodies retain their exact authenticated text. Value-shaped return items (such as literals, concepts, composites, event-property bindings or `context` members) are also refused with `STAGE-ESM-037`: they are values, not modeled events or commands, and would fail at run time. Reading those values as constructor arguments of a returned modeled event or command is supported; returning `null` or an empty collection still appends nothing.

Bodies cannot be combined with declarative `produces`, `invokes`, decision `reads`, additional triggers or `runs as`. The compiler refuses body-dependent decision reads; unsupported render shapes retain `STAGE-ESM-024`. Existing renderable `where` guards run before the body, and semantic `from` filters remain reactor attributes. Declarative reactions retain their existing output and restrictions.

Stage re-indents the authored body to the generated block as described in [Indentation of spliced authored bodies](https://github.com/Cratis/Stage/blob/main/Documentation/reference/stage-rendering.md#indentation-of-spliced-authored-bodies); literal lines and line breaks are unchanged.

Attachments must retain their compiler requirement identity, reaction-effect role, slice owner, language, supported context/result versions and content hash. Authentication covers resolved UTF-8 text, not proof of original disk bytes. Rendering preserves that text through the inline splicer. Structural escapes, unsupported containment and foreign-language bodies are refused. Declarations colliding with generated parameters or property locals are refused without renaming either side. This declaration check includes nested locals, patterns, deconstruction, foreach/catch variables, parameters, local functions and query range variables. Stage compiles the rendered reactor at render time against the model's events in their slice namespaces, the generated concepts and Chronicle's `EventContext`, and refuses a body that does not compile with `STAGE-ESM-012`.

Authored C# is trusted native code, not sandboxed code. Reflection calls are not structural escapes. Rendering and compiling a body do not execute it or prove runtime behavior. Screenplay's reference evaluator and the semantic Host still report opaque bodies as Unsupported: their shared admission retains the opaque-body refusal, and only Cratis render admission excludes the supported subset. Successful rendering is not an execution or conformance pass. Generated reaction specifications retain their separate admission limits. This modeled-execution limitation holds for the event trigger as well as the startup trigger, and for the inline and the file form: the independent specification executor and the semantic Host refuse them as Unsupported, name `STAGE-ESM-024`, and append nothing. It is not a restriction on running the generated Cratis application: supported event-triggered inline and file bodies compile as native synchronous handlers, and Chronicle executes them against the triggering event. A conditional body can append an event on the triggering source or return an empty sequence without appending anything. Native handler execution does not make the same opaque body executable by the reference evaluator or semantic Host.

What Chronicle and Arc do with the value an authored body returns is fixed by those packages, not by Stage. Chronicle registers the generated synchronous `IEnumerable<object>?` method as a reactor handler. A `null` return or an empty sequence appends nothing. A sequence whose elements are all registered event types is appended to the triggering event source. A non-empty sequence of `EventForEventSourceId` envelopes is also supported: Chronicle appends it as one transaction, preserving each envelope's destination and append metadata. A non-empty sequence of only commands is executed by Arc's command result handler. A sequence that mixes events and commands goes through Arc's `EventsAndCommandsResultHandler`, which appends the events before it runs the commands. That is not atomic, so a redelivery can append the events again, which is the hazard `STAGE-ESM-037` avoids for declarative reactions. A value that no registered side-effect handler claims, such as a string, an `EventContext` or a sequence containing an unsupported type, fails the partition with `UnhandledReactorReturnValue`. Stage rejects mixed, undeterminable and value-shaped returns with `STAGE-ESM-037` before publishing artifacts, rather than knowingly emitting a handler that will fail the partition.

The pinned-kernel probe renders inline and `file` reaction bodies, builds the generated application, and runs it against the same Chronicle development image as CI. It confirms that returned registered events are appended to the triggering event source and an empty return appends nothing. The probe also asserts, without a connection, that a returned string is refused at render time and publishes no artifacts.

## Atomic batch rendering

With `reaction batches atomic` (ESM v22+), Stage admits a generated event handler with at most one
trigger that directly produces events. Its complete returned `IEnumerable<object>` contains the
`EventForEventSourceId` envelopes in production order; Chronicle appends that collection as one
transaction. Explicit `for` destinations, tags and occurrence metadata stay on each envelope. A false
`where` guard returns no productions. No earlier event is appended while a later mapping is evaluated.

Each selected invocation refusal branch likewise returns one event collection, which Arc appends as a
separate batch. Invoked commands remain separate units; a handled refusal stops later invocations.
Neither a refusal-branch batch nor a direct-production batch rolls back an already committed command.
The generated application remains eventually consistent; this does not introduce the Host's
whole-command rollback boundary.

Multiple direct-production triggers on the same event in one reaction fail `STAGE-ESM-043`, even if their
conditions appear mutually exclusive: combining them into one returned collection would combine their
modeled batch boundaries. A Chronicle reactor appends what it returns as one batch, and appending several
batches from one handler without hand-written idempotency keys would duplicate the earlier ones when a
later batch is rejected and the event is redelivered. Stage renders these triggers once Chronicle can return ordered per-trigger batches
([Chronicle#5248](https://github.com/Cratis/Chronicle/issues/5248)). Screenplay rejects routes on direct and refusal-branch productions before
Stage admission; Stage also retains a defensive `STAGE-ESM-043` guard for routed direct productions.
Screenplay does not yet represent atomic stream closing (Screenplay#770), so no closing operation is
emitted. Existing mixed-effect and specification restrictions still apply. The legacy syntax renderer
continues to refuse the atomic declaration.

Models without the declaration retain their existing generated bytes and admission rules. In
particular, legacy same-event triggers continue to share a returned collection; the renderer does not
claim that this reproduces the legacy reference executor's per-production partial-commit boundary.

## Ordering, retry and catch-up

Within a handler, Stage collects effects in authored trigger and mapping order. Arc executes a returned command collection in order and stops at its first failure. Separate reactors are independent observers: do not rely on the order in which their follow-up events arrive.

For event reactions, `runs as system` supplies Arc's system principal only to returned commands. Role-only and authenticated-only authorization gates render. A claim-reading gate, including a claim under `not` or `or`, or an opaque gate under `runs as` fails with `STAGE-ESM-034`: Screenplay treats system claims as unknown, while Arc supplies `[System]` subject claims. Generated policies are unchanged.

A denied, rejected or throwing invoked command fails the live reactor partition and can be retried. Screenplay's reference execution instead ends the scenario at that failure. A reaction without `runs as` can render while invoking an authorization-gated command, but the live invocation has no principal and can leave a failed partition. Rendering is not proof of successful authorization or settlement.

A newly registered reactor catches up from the existing event log. `[OnceOnly]` prevents replay; it does **not** deduplicate ordinary deliveries or retries after failure. Keep commands and external effects safe for repeated delivery, and inspect failed partitions during operation.

## Specifications and verification

The in-memory executor runs event-reaction specifications without building a generated application.
A specification runs only the slice that declares it (Screenplay decision 0115): other slices' reactions and
publications do not run, and a command another slice owns, invoked by this slice's reaction, is decided (its
refusal routes to `on refused`) but its events are not kept. See
[A specification runs its own slice only](../docker/spec-runner.md#a-specification-runs-its-own-slice-only).
Given events establish history without firing reactions. Direct-append then-events describe only the
reaction effects; command then-events include the initiating command's facts and the cascading effects of
reactions in the same slice.
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
Generated clock/trigger/capture specification actions and scheduling triggers remain unsupported; application-trigger occurrence rendering does not add generated `when trigger` scenarios.

Stage renders single-hop `when append` reaction specifications through `ReactorScenario`. Givens establish history and are not delivered to the reactor. A production-only scenario checks the exact effect count, payloads, destinations and all three unrouted metadata values (`EventSourceType.Default`, `EventStreamType.All`, `EventStreamId.Default`), with assignment matching for any-order expectations. An invocation scenario executes the one returned command through `CommandScenario`, reading the system identity from the reactor's attribute rather than forwarding `given caller`. Screenplay requires an outcome: a `when append` specification always asserts the reaction's produced events or the invoked command's outcome.

Append fixtures may carry an explicit source and route. A sourceless append uses a fixed scenario placeholder only when no production relies on the trigger destination. Fan-out, deeper cascades, multiple invocations, `$context`, read-model/query expectations and constraint-target appends or products remain refused with `STAGE-ESM-011`, with the exceptions below. Read-model absence expectations in reaction-settling scenarios retain `STAGE-ESM-011`; projection-only specifications over given facts or a non-reactive append can assert keyed absence through `ReadModelScenario.Instances`. Other non-reaction appends remain refused. The append values must select exactly one active trigger after `from` and `where` are evaluated; an excluded append fails admission with `STAGE-ESM-011`. Guarded alternative paths can each be specified with their own outcome.

An append the event log itself refuses is rendered too. `when append <event>` followed by `then error "<message>"`, where the event is the target of exactly one constraint whose message (or name, as the error code) is the one expected, becomes a scenario that seeds the given events, appends the event through the in-process event log and asserts that the append failed with that constraint, with that message, and that nothing else was appended. The given events must really make the append violate the constraint (the same event source for `unique event`, another source holding the same value for `unique <property>`); a constraint over several properties, a routed or sourceless append and any other expectation (events, response, read model, caller, clock) stay refused with `STAGE-ESM-011`, naming the reason.

A refusal scenario (a reaction whose invocation has `on refused` branches) runs the invoked command through the real Arc pipeline. A command with generated properties renders when every generated property is pinned with `given generated <Command>.<property> = <value>`: the scenario pins the value (`PinGeneratedValue`) before the command runs, so the ids the expectations name are known. A command that reads (`reads`) runs in a scenario typed by that command so Arc can read the decision state the given events seed, and what the reaction appends is read back from the event log. A constraint-target event among the *invoked command's* productions is enforced by the command pipeline (a refusal by that constraint selects the `on refused by constraint` branch); a constraint-target event among a *refusal branch's* productions is appended straight to the log and stays refused with `STAGE-ESM-011`. A protected or generated-property command renders only when it is the reaction's one invocation. When the reaction's one invocation is a command another slice owns, the rendered specification asserts only that the command is decided (succeeds); it does not assert that command's events, which its own slice's specifications cover.

`given generated <Command>.<property> = <value>` on a specification whose action is that command pins the generated value of the action itself, exactly as if the action had stated it; renderer, admission and executor all read it that way, so the event source a `then` names for a generated id is a known value rather than a draw.

The generated `CommandScenario` does not settle reactions, whereas the reference settles those of the specification's own slice. When a command specification produces exactly one event that fires exactly one invoke-only reaction, Stage splits it: an accepted specification becomes a command half that asserts the triggering events and a `ReactorScenario` half that appends the triggering event and asserts the rest; a rejected specification becomes only the reaction half, and only when the triggering command cannot reject on its own (no validations, requirements, decision reads, routes, authorization, generated values, constrained or constraint-releasing products, rule-carrying concepts, and matching input values). Every other command specification whose command produces an event subscribed to by an event reaction stays refused with `STAGE-ESM-011`. The split is not atomic: the reference rejects the whole command when the invoked command rejects, while a rendered application commits the triggering command and its reactor fails afterwards.

Generated backend `$context.occurred` mappings remain supported through `EventContext.Occurred`, but
context-dependent generated reaction specifications retain `STAGE-ESM-011`. Authored clocks on generated
append/redelivery specifications additionally retain `STAGE-ESM-024`; the renderer's clock fixture support
remains command-only. These specification-scoped diagnostics are warnings during artifact planning:
Stage omits the unrenderable specification while preserving renderable specifications and production
artifacts. Each warning names the specification, original code and reason. A production-scoped
refusal still fails the whole render. The constraint-target guard remains unchanged. See
[Render around refused specifications](stage-rendering.md#render-around-refused-specifications).
Independent executor support does not broaden that generated scenario contract.

Render admission, generated Debug compilation, generated specifications and live execution are separate results. Build and test the generated project rather than treating a published render as runtime evidence. Live verification requires a Chronicle kernel and the generated Arc host wiring, including its reactor result handlers.

## Limits and diagnostics

| Diagnostic | Refused shape |
| --- | --- |
| `STAGE-ESM-024` | Application-trigger reactions outside the occurrence subset above (including `on refused`, authorized commands without `runs as`, and production-only `runs as`); startup/shutdown triggers pending an activation identity and namespace policy; interval/schedule triggers pending due-time parity; reaction bodies outside the single-event render-only subset above; captures and Translate shapes outside the [translation subset](translation-rendering.md); clock/trigger/capture specification actions; generated append/redelivery specifications with clock fixtures |
| `STAGE-ESM-034` | `runs as` invoking a claim-reading or opaque authorization gate; a reaction without `runs as` that invokes commands both with and without an `authorize` clause; all `runs as` on legacy syntax paths |
| `STAGE-ESM-035` | Reaction `from` on legacy syntax paths only; use the semantic planner |
| `STAGE-ESM-030` | Reserved stored source or stream names in observer filters |
| `STAGE-ESM-037` | Direct productions combined with event-producing refusal branches in one trigger ([Arc#3561](https://github.com/Cratis/Arc/issues/3561)); mixed productions and invocations across separate triggers on the same event in one reaction |
| `STAGE-ESM-043` | Atomic models with multiple same-event direct-production triggers in one reaction; a defensive guard for routed direct productions (Screenplay already rejects reaction routes); all atomic declarations on legacy syntax paths |
| `STAGE-ESM-013` | Reaction mappings of `$context.causedBy.*`, `$eventSourceId` or `$eventContext.eventSourceId` |
| `STAGE-ESM-039` | Conditions, destinations or mappings that cannot retain their modeled meaning; incomplete event mappings or a reaction without an event subscription |
| `STAGE-ESM-012` | Reactor names or handler names that collide with generated C# declarations or slice files, or the authored handler does not compile in its rendered reactor |
| `STAGE-ESM-011` | Append specifications outside the single-hop subset above; command specifications that cascade into reactions |
| `STAGE-ESM-007` | A query or projection in an Automation slice without a locally declared read model and its single projection or reducer |
| `STAGE-ESM-009` | A flat projection whose payload key cannot be proven equal to the append destination for every command and reaction producer |
| `STAGE-ESM-040` | Personal or secret concepts used as reaction production destinations |

Conditions cannot use optional, composite, collection or enum-valued operands; ordering requires numbers. Property mappings must retain the exact source and target type. Commands, projections, reducers and queries declared in an Automation slice retain their existing admission limits. Query-only and projection-only surfaces are checked like StateView slices, not silently omitted. For a flat projection, every reaction production of its observed event must explicitly target the same value mapped into the payload key. An omitted `for` uses the triggering event source and does not prove that equality. This check includes producers outside the selected render scope. Personal and secret concepts cannot supply append identities; use a surrogate destination and keep the protected value in the payload.

Stage admits one trigger with direct productions and command invocations, including `acknowledge`-only refusal handling, in both default and `reaction batches atomic` models. The handler returns direct events before commands, preserving each list's order and destinations. `[OnceOnly]` is retained. The pinned Arc result handler gives direct appends stable delivery-based idempotency keys, so redelivery does not duplicate them. Stage does not generate its own keys.

Screenplay retains direct productions already accepted before a later command refuses; v19 stops later invocations on an unhandled refusal without rolling back that prefix. Atomic production mode makes the direct production batch atomic, not the entire reaction. Stage retains `STAGE-ESM-037` where direct productions coexist with an event-producing refusal branch: refusal-branch events can reuse direct-event idempotency keys ([Arc#3561](https://github.com/Cratis/Arc/issues/3561)). Mixed effects across separate same-event triggers also retain 037 because Cratis appends all events before commands and stops the command list on refusal, unlike Screenplay's per-trigger declaration order. Splitting or combining handlers would change that order; Stage does neither. See [Stage#342](https://github.com/Cratis/Stage/issues/342).

Per-production route overrides retain `STAGE-ESM-036`; Screenplay already refuses reaction production routes during binding.

Scoped rendering may reference an invoked command or triggering event in an unselected slice, as it does for cross-slice event references. Include those dependencies when compiling the application.

See [Build a renderer target](../guides/build-renderer-target.md) for the full admission table and [Customize a rendered application](../guides/customize-rendered-application.md) for unmanaged extensions.
