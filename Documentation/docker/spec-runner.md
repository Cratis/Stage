---
title: The specification runner
description: Running the specifications modeled in a Screenplay model as a container job, and reading the results.json it writes.
---

The specifications in a Screenplay model are part of the model, not a separate test project — so they can be run
without building anything. `cratis/stage-specrunner` is that run: a **run-to-completion job** that compiles the
model, verifies its specifications, writes `results.json`, and exits.

Unlike [the Stage container](index.md), it starts no server and needs no event store. It is meant for a build
pipeline or an editor's "verify my model" action. The default structural engine is **deprecated**: it checks
model consistency, not behavior, and will be removed in the next major version. This minor version retains
`structural` as the default for existing `results.json` consumers; each structural run prints one deprecation
notice to standard error. Structural runs refuse named event sources, streams and event-source routes with
`STAGE-ESM-030` on standard error, exit `1`, and write no results file. The semantic engine executes routed givens and when-appends, compares event routes and `no stream`
expectations, and matches any-order expectations by assignment at ESM v8 and later. The structural engine refuses public-event
publication (`STAGE-ESM-032`), foreign events and `source events` captures (`STAGE-ESM-033`), and directed
Translate slices (`STAGE-ESM-024`, naming the direction). Both engines refuse reducer observer filters (`STAGE-ESM-035`).
The structural engine still refuses every reaction execution identity (`STAGE-ESM-034`) and reaction observer filter (`STAGE-ESM-035`); semantic renderer support for reaction filters and role-only or authenticated-only `runs as` invocations does not relax these structural refusals. Per-production route overrides run on the semantic engine; an override on an unkeyed stream under a keyed command route, or with the literal stream id `Default`, is not admitted yet and fails `STAGE-ESM-036`; the structural engine refuses their stream syntax with `STAGE-ESM-030`.
The semantic engine runs Translate slices. An inbound `source events` capture appends its private event for the capture key, following the Screenplay reference: the first observation of a key counts only as `added`, and field-change and transition conditions compare it with the previous observation. An outbound projection that targets a public event appends that event to the run's own event log when its content changes; publishing to another application's inbox is not part of a run. Translation refusals are scoped to each specification, so an unrelated Translate slice does not block a run. The semantic engine still refuses, with the codes above, captures that use `map`, children or nested values, `given capture` on a `source events` capture, reducers that target a public event, public-event projections with `every`, `clear` or a `from` that leaves a required property unmapped, and reactions or read-model projections declared inside a Translate slice.
A specification runs only the slice that declares it (Screenplay [decision 0115](https://github.com/Cratis/Screenplay/blob/main/decisions/0115-a-specification-runs-its-own-slice-only.md)); see [A specification runs its own slice only](#a-specification-runs-its-own-slice-only).
Within that slice, the semantic executor settles event-triggered reactions after each command or direct append, including
`where`, productions, invocations, cascades, `runs as`, `from`, mixed effects and append constraints.
It processes new facts in append order, reactions in semantic-identity order and triggers in authored order,
producing events before invoking commands. Given events establish history without firing reactions.
An invocation uses the declared system roles, or no caller; it never inherits `given caller`, and system
claims remain unknown. Invoked generated commands require unavailable deterministic fixtures and return
`Unsupported` for `IdentityAllocation`. More than 1000 new facts returns `Unsupported` for `Reaction`.
Command, direct-append and named-redelivery specifications accept a millisecond-aligned `given clock` no earlier
than the Unix epoch. Offset-bearing source clocks normalize to canonical UTC. That authored instant supplies
`$context.occurred` throughout reaction productions, cascades, invocation arguments, invoked commands and
refusal productions, formatted as UTC round-trip text such as `2027-03-04T08:00:00.1230000Z`.
Without `given clock`, a reached reaction production using `$context.occurred` rejects with
`Reaction '<name>' uses $context, which needs the scenario's 'given clock'.` An invocation argument using it
rejects with `An execution expression is unresolved in its declared root scope.` A false `where` skips these
evaluations. The executor never substitutes wall time or its injectable clock for a missing reaction occurrence.
This time-only fixture supplies no audit identity, including under `runs as system`: reaction mappings of
`$context.causedBy.*` retain `STAGE-ESM-013`. A reaction can map `$eventSourceId` (or `$eventContext.eventSourceId`): it is the event source of the event that set the reaction off, and the target property must be text, a uuid, or an identifier concept over one of them.
The spelling `$context.eventSourceId` is rejected by the compiler.
The executor also runs `when trigger` specifications for application, startup and shutdown triggers, and
`when clock` specifications that advance a canonical `given clock` to a later instant. A fired trigger takes its
values from the fixture and its occurrence from `given clock`; each reaction declaring a matching trigger fires
in reaction order and its effects settle like an event reaction. Advancing the clock fires every interval and
schedule occurrence due in the window after `given clock` up to and including `when clock`: intervals are
multiples of their period counted from the Unix epoch, schedules are UTC times of day with an optional day of the
week or month. Occurrences fire in instant order, then reaction order, then trigger declaration order, and each
supplies its own instant as `$context.occurred` to its productions, cascades and invocations. More than 10,000
occurrences in one advance returns `Unsupported`. Opaque trigger bodies and capture specifications remain refused
with `STAGE-ESM-024`, and the live semantic Host and rendered applications still refuse clock, application,
startup and shutdown triggers. Generated backend occurrence mappings remain supported, but
context-dependent generated reaction specifications retain `STAGE-ESM-011`, and clocks on generated
append/redelivery specifications retain `STAGE-ESM-024`; renderer clock fixtures remain command-only.
The executor uses its own reaction loop. Unlike the [semantic Stage Host](index.md#opt-in-to-semantic-execution),
which refuses the whole live batch if settlement fails, a specification ends at its first rejection or
unsupported capability. Rendered Chronicle reactors run eventually and retry failed partitions.
Structural refusals exit `1` without writing results;
semantic runs record `Unsupported`, never a passing specification. The semantic executor uses Screenplay 4.247.3, admits exact ESM v1–v30 and v32–v34
version pairs (not mixed/noncanonical pairs) and refuses unaudited language/semantic versions (v31 and v35 or later) with `STAGE-ESM-016`.
At v33 a capture that keys its records by event source is refused with `STAGE-ESM-016` when the specification's slice runs it;
at v34 `then invokes` is compared as described below. The renderer and the live semantic Host admit exact v33 models (the renderer renders `key is event source`; see [Translation rendering](../reference/translation-rendering.md)) but still refuse v34 models whole with `STAGE-ESM-016`. The Host has no capture runtime of its own, so a v33 capture keyed by event source does not block live admission; only the specification check above refuses it, until Stage#842 lets the executor run it.
A v22 model with `reaction batches atomic` executes each direct-production batch all-or-none; see [Atomic reaction batches](#atomic-reaction-batches).
At v28 the semantic executor runs Uuid `==` and `!=` in command requirements and `produces when` guards, including protected
read paths, with the reference evaluator's results: Uuids compare ordinally in canonical lowercase hyphenated spelling, and a
non-canonical Uuid input is rejected before the requirement runs. The legacy structural runner refuses Uuid comparisons with
`STAGE-ESM-049`.
At v27 a specification that projects into a read model with several `key` parts records `Unsupported(Projection)`,
naming ESM v27 composite read-model keys: the per-run scenario seeds and looks up instances by one event-source key.
The semantic Host runs such read models, and the Cratis renderer emits generated specifications only for command and event scenarios that do not project into them (a specification that projects into one is refused with `STAGE-ESM-011`); the legacy structural runner refuses them with `STAGE-ESM-048`.
The semantic executor runs per-run projections whose read models hold composite or collection properties, mappings from a member of a composite event value (`city = address.city`), products of two event values (`add total by unitPrice * quantity`, carried on the stored event as a computed member), flat transitions keyed by an event property with literal mappings, captures with child collections, public events published from several source facts, and scoped projections keyed by the property a capture or the event names, including text event sources that hold a Uuid. A DateTime read-model value compares as the same instant and offset, so `Z` and `+00:00` agree; a composite or collection value that authors an explicit null member stays `Unsupported(Projection)`, because the typed per-run models cannot tell it from an absent member.
At v26 the semantic executor runs chronological Date/DateTime command requirements, conditional productions
and validation rules (including concepts). DateTime equality compares UTC ticks; Date compares calendar days.
Participating DateTime inputs without an explicit offset or `Z` reject after authorization, before other input
validation, including reaction invocations. Specification `then` matching remains ordinal. A command combined
with opaque validation reports `Unsupported` at run admission, before execution. The Host also refuses opaque
validation before authorization and input validation. Unlike both Stage paths, the reference checks authorization,
then offsets, then opaque validation: an offset-less input combined with opaque validation returns `Validation`
in the reference but `Unsupported` in Stage. `Unsupported` never counts as a pass. The renderer
render date comparisons, and the legacy engines refuse them with `STAGE-ESM-046`. Date three-way conformance
covers the canonical corpus in all source forms.
The semantic executor and Host run protected command reads, including reaction-invoked commands. After authorization and
property/concept rules, both require each declared view at its key in the projected prior facts.
Removal and recreation are preserved. Root removals may use the event-source identity or one required
scalar event property whose value type matches the read-model identifier. Property-keyed removals use that
property, not the event source, and preserve instances at other keys. Optional, missing, composite and
parent removal keys remain refused. Empty, whitespace-only,
exactly `*`, containing `#`, and leading- or trailing-whitespace keys reject before projection lookup, even if an
instance is stored at that key. These refused keys and absent views reject validation before all requirements and
generation, with `<View> '<key>' does not exist.`, preserving the supplied key text. This refused-key check is specific
to protected reads; command identifiers keep their existing behavior. The Host delegates to the pinned
Screenplay 4.247.3 evaluator with the same refused-key checks. Read-backed requirements and
produced-event mappings use that projected state. Non-Error requirements on protected commands remain
`Unsupported` with `STAGE-ESM-045` until Screenplay defines their severity semantics. This admission check includes
commands reachable through reaction invocations, even in specifications expecting rejection, and runs before any facts are appended.
Scoped projections also run one-level identified child collections: event-property child keys, event-source or required event-property parent keys, child removals and identity joins. Appends preserve first-insertion order; updates and joins do not reorder children. A child event or child removal whose parent never appears in the run is dropped, as in the reference; a child event that arrives before its parent does is refused by name (existing parent instance required). Root joins are supported too. A join runs only when every event it reads has an event-source identity of the joined key's value type (text or UUID) and enumeration values; otherwise the specification is refused, because the runner passes identities as text and could not tell a text `"1"` from a number `1`. Join removals, nested scopes, nested child collections, child `every` blocks and read-model fixtures combined with children or joins remain refused.
A product mapping is refused only when the run reaches the `from` block's event; a product block whose event the run never delivers is skipped, and an observed one stays refused.
The renderer renders protected reads and refuses only the shapes it cannot preserve with `STAGE-ESM-044`
(see [Protected reads rendering](../reference/protected-reads-rendering.md)). Generated applications do not yet enforce
the refused-key checks; the six canonical refused-key specifications remain known renderer differences.
The structural engine refuses the preamble,
exits `1` and writes no results file.
Without the preamble, command reads still fail binding with `PLAY0271` and the repair diagnostic `PLAY0786`.
At v20 it executes rules on generated concepts. A failing fixture is a contract rejection with
`Generated fixture '<property>' fails concept '<Concept>' rules`, not the rule's authored input-validation message.
Each supplied fixture is checked for shape and rules before the next fixture or a missing fixture.
Generated concept rules render at every severity; the pinned Arc runtime rejects any failing generated rule as a server generation defect.
Explicit concept generation, not the schema version, activates strategy shape checks and generated-source freshness;
implicit-UUID-only models remain unguarded at later versions. A generated concept with rules still requires its fixture's canonical strategy shape.
See [Generated values](../reference/generated-values-rendering.md#rules-on-generated-concepts) for rendering refusals and Host exception behavior.
At v11 it executes retained exact Double-mode integer and fractional values without a second rounding
step, including the safe whole-number bounds ±9007199254740991 and `0.10000000000000002`.
This does not enable `numbers exact` or extend the source integer range.
At v12 it executes specifications for numbered enums with unchanged name-based comparisons.
The executor validates query keys and supplied filter values against concept rules, including
nested members and collection elements, after authorization. This applies in every admitted ESM
version. ESM v18 adds query-only error expectations, which require a Validation-category rejection;
the trace records `rejectionCategory` for rejected queries. Empty-fixture queries also execute valid
unmatched keys and omitted optional filters. Equality-filtered query assertions execute over projected
and seeded read-model state. Supplied scalar primitive, concept and enum filters combine with `by`
using AND; omitted optional filters do not narrow. Collection results compare by read-model identity
and key without regard to order, including row counts. Projection shapes the per-run scenario cannot
preserve remain `Unsupported` with a typed reason.
The Host admits these specification runs and executes live query filters and validation through the reference evaluator.
At v13 it executes concept-compliance specifications with unchanged outcomes; a passing specification
is not evidence of encryption or redaction. The semantic Stage Host encrypts personal-data and secret
concept values at rest through Chronicle, like rendered applications, and releases them on read.
Personal-data erasure uses Chronicle's key deletion keyed by the event source; explicit subject lineage
remains [Stage #28](https://github.com/Cratis/Stage/issues/28). Existing Play stores retain earlier plaintext events.
The semantic executor, Host and renderer admit text and non-text `secret` concepts (`Int`, `Decimal`,
`Bool`, `Date`, `DateTime`, and `Uuid`), preserving their primitive types. `pii secret` uses PII only.
Generated applications carry protection attributes, and renderer admission refuses protected identity
uses or enums with `STAGE-ESM-040` ([Concept compliance rendering](../reference/compliance-rendering.md)).
Migrate to `--engine semantic` and the semantic report format described below.

```bash
docker run --rm \
    -v /path/to/screenplays:/model:ro \
    -v /path/to/results:/output \
    cratis/stage-specrunner:latest
```

```text
Ran 3 specification(s) for event model 'Invoicing'. Results written to /output/results.json.
```

## A specification runs its own slice only

A specification states what its own slice does, so the executor isolates the run to that slice, as Screenplay 4.246.0
and later do:

- Other slices' reactions do not run, whether an event, clock advance or trigger would select them. Only an explicit
  `when redelivered ... to <Reaction>` still delivers to the named reaction.
- Other slices' captures and outbound publications (projections that target a public event) do not run, so their
  admission refusals do not refuse the specification either.
- A command another slice owns, invoked by this slice's reaction, is still decided: validation, authorization,
  constraints and protected reads run, and a refusal routes to the reaction's `on refused` branches. Its events are
  not kept: they are not appended to the run's in-memory Chronicle event log, so neither `then` events, read-model
  and query expectations nor later reactions see them. The command's own slice specifies its facts.
- Read-model projections still build what the specification reads, from the facts the run keeps.

From ESM v34 a specification can state invocations with `then invokes <Command>` and the values it expects. Every
invocation this slice's reactions make is recorded, accepted or refused. When stated, the invocations must match in
count and order (as a multiset under `then events in any order`), and only the stated values are compared. A mismatch
fails with the reference runner's messages, for example
`Invocation at index 0 does not match the expected command and values. Expected: CloseInvoice(reason = "cancelled"). Actual: CloseInvoice(id = "one", reason = "paid").`

## Recovery and empty-effect specifications

At ESM v17, `when redelivered` selects an existing given occurrence and delivers it only to the named
reaction. The semantic executor applies that reaction's observer filter and `where`, without appending the
input again or delivering givens to sibling reactions. It then settles all observers of newly produced facts,
including invoked commands and downstream cascades. A nonzero given position selects that occurrence,
not the first event of the same contract. An unhandled invocation refusal can be asserted with `then error`.
An authored `given clock` supplies the redelivery occurrence, not a timestamp from the selected given event.
The same instant reaches new effects and cascades. Audit-identity, generated-value and projection capability
limits still apply; redelivery is not replay or a guarantee of exactly-once external effects.

`then no events` asserts an empty new-fact set on a successful run. For commands it counts all command and
cascade facts. For redelivery it counts only new effects, not givens. For `when append` it excludes exactly the
initiating append and counts every following effect. It may accompany response, read-model or query
expectations, but not event, event-order, error or denial expectations. A nonempty result fails the specification;
omitting event expectations in an older model does not acquire this assertion.

This execution support is broader than generated specifications. Rendered redelivery supports one event
trigger on the named reaction: either direct productions or one command invocation supported by the existing
command harness. Stage uses its central Chronicle pin and records every given through `ReactorScenario.History`,
then calls `Redeliver` on the selected occurrence. Its original sequence identity, context and stream route
are reused; history is not delivered to sibling reactions and the input is not appended again.

Generated empty-effect specifications support admitted commands and single-hop production-only reactions,
including excluded `where` or observer filters on appends and redeliveries. Redelivery requires explicit given
sources and portable stream identities. Fan-out on the named reaction, deeper cascades, constraints,
read-model/query expectations and redelivery with invocation refusal branches retain `STAGE-ESM-011`.
Reaction-triggering commands are split into a command half and a reaction half only for one event firing one
invoke-only reaction (rejections only when the triggering command cannot reject on its own); other
reaction-triggering commands, invoked-command empty-effect cases and no-observer appends also require
a settlement/log-delta harness and are refused. See the [renderer support matrix](../guides/build-renderer-target.md#admit-target-semantics-before-emission).

## Atomic reaction batches

With `reaction batches atomic` (ESM v22+), the semantic executor validates every direct production of
one trigger occurrence before appending any of them. A selected refusal branch's productions form their
own atomic unit. A mapping failure, constraint violation, projection refusal or insufficient remaining
fact budget appends none of that unit; earlier successful units remain. Invoked commands are separate
units, not part of the trigger's direct-production batch. Constraint refusals preserve the authored message
and constraint name.

Property-value claims reserved within a batch remain reserved even if their owner changes value later
in that batch. Repeating a value for the same owner is allowed; an event-occurrence release can reopen
its owner's cycle. Models without the declaration keep legacy per-fact reaction execution, including
successful prefixes before a later production fails. The [live semantic Host](index.md#opt-in-to-semantic-execution)
retains its stricter whole-command rollback boundary. For what the Cratis renderer emits, and the atomic shapes it
still refuses with `STAGE-ESM-043`, see [Atomic batch rendering](../reference/reaction-rendering.md#atomic-batch-rendering).

## Invocation refusal branches

At ESM v19, event-reaction invocations can handle business refusals with ordered `on refused` branches.
The first matching branch wins. Bare `on refused` matches validation or constraint refusals, never
an authorization denial; `by authorization` must be explicit. A named constraint selector matches any
violated name, while `$refusal.constraint` exposes the first violation name. `$refusal.reason` is
`validation`, `constraint` or `authorization`; `$refusal.message` joins error validation or constraint
messages in reference order with newlines, preserving whitespace and unresolved localization keys.
Warning-only rejections, contract defects, unsupported capabilities and infrastructure/concurrency failures
are not catchable business refusals.

A branch either acknowledges without new facts or appends its declared productions through ordinary
constraint and projection checks, using the same authored occurrence as its triggering invocation.
It then skips the remaining invocations of that trigger, not sibling
triggers, other reactions or queued cascades. Earlier committed facts remain. A rejected branch append
ends the scenario; it is not offered to a later refusal branch. Invocations still use the reaction's own
system identity or no caller, never the fixture caller. Cancellation and the 1000-new-fact limit remain
non-passing outcomes.

The independent executor runs refusal branches, including named redelivery and downstream cascades.
Generated specifications have a narrower append-only, single-hop subset: scalar invocations without
unavailable generated fixtures, ordered terminal unconstrained productions or acknowledgement, explicit
append source, no routing/context/read assertions, and exactly one selected trigger. They execute the
returned Arc wrappers with the context-aware command side-effect executor and compare persisted log
deltas after settlement. Redelivery, fan-out, deeper cascades and constrained output scenarios still fail
rendering with `STAGE-ESM-011`; this includes the unmodified v19 invocation-refusal corpus. Same-source
multi-trigger refusal groups fail rendering with `STAGE-ESM-039`, because Arc stops a returned command
list rather than one Screenplay trigger subgroup. Generated refusal scenarios also refuse coded or
multiple error expectations with `STAGE-ESM-011`. Mapping `$refusal.message` from a command with
localized validation or constraint messages, or `$refusal.constraint` with several potentially violated
constraints, fails rendering with `STAGE-ESM-039`: Arc 22.88.0 cannot preserve unresolved keys or
Screenplay's first-violation ordering. These rendering limits do not narrow the independent executor.

## Mount points and arguments

The image defaults to the two mounted folders, so the invocation above needs no arguments:

| Argument | Default | Meaning |
|---|---|---|
| `--model <file-or-folder>` | `/model` | One `.play` file, or a folder searched recursively for `.play` files and compiled as one application. |
| `--output <file>` | `/output/results.json` | The file the results are written to. |
| `--slice <guid>` | — | Limit the structural run to a single slice (structural-only). |
| `--spec <guid>` | — | Limit the structural run to a single specification. |
| `--engine semantic` | `structural` | Opt in to executable semantic specifications and the versioned semantic report. |
| `--specification <semantic-id>` | — | Select a semantic specification (semantic engine only). |
| `--scope <semantic-id>[,<semantic-id>...]` | — | Select application, module, feature, slice or specification identities; repeatable (semantic engine only). |
| `--catalog <file>` | — | Use an authoritative semantic identity catalog (semantic engine only). |
| `--application <name>` | Input folder/file name | Preserve the application name across input layouts (semantic engine only). |

Override either default by passing arguments after the image name. To compile just one file, mount only that
file read-only; no host parent-folder mount is needed:

```bash
docker run --rm \
    -v /path/to/invoicing.play:/model/input.play:ro \
    -v /path/to/results:/output \
    cratis/stage-specrunner:latest \
    --model /model/input.play --output /output/invoicing.json
```

File extensions are case-insensitive. A single-file input does not load sibling `.play` files. Implementation
`file` references remain symbolic and are not opened or executed by this loader. For local use, the executable
requires both `--model` and `--output`; the defaults above are supplied by the container.

## Exit codes

| Code | Meaning |
|---|---|
| `0` | The run completed and `results.json` was written. **A failing specification is still a completed run** — read the outcomes from the file. Semantic runs execute event-source routes; structural runs refuse them. |
| `1` | The input path is missing, the file extension is unsupported, the folder is empty, model compilation fails, or a structural run refuses event-source routes (`STAGE-ESM-030`), public events (`STAGE-ESM-032`), foreign events or source-events captures (`STAGE-ESM-033`), directed Translate slices (`STAGE-ESM-024`), any reaction execution identity (`STAGE-ESM-034`, unchanged by renderer admission), or observer filters (`STAGE-ESM-035`, unchanged by semantic renderer support). Semantic runs refuse a production route override on an unkeyed stream under a keyed command route, or with the literal stream id `Default`, with `STAGE-ESM-036` in their report; neither shape is admitted yet. An actionable error is written to standard error; no results file is written. |
| `2` | A required argument (`--model` or `--output`) was missing. A usage line is written to standard error. |

Input failures do not write results or delete or overwrite an existing output file. Compiler errors identify the
source path and position, ordered by path, line and column. Folder diagnostics use relative source paths.

The existing argument parser treats an invalid `--slice` or `--spec` GUID as no filter, rather than rejecting it.
Check filter values before invoking the runner; this input-path support does not change that policy.

## Reading results.json

The file carries the model's identifier and one result per specification, each with its per-step outcomes:

```json
{
  "eventModelId": "cacf0ce6-f6bc-9300-c909-657aa5b1cbb8",
  "results": [
    {
      "sliceId": "9f877a6c-f978-e3c5-f3d4-b6d23a0bc11c",
      "specificationId": "22719326-4950-add1-7947-98eee1a11d2a",
      "specificationName": "RegisteringADraftInvoice",
      "sliceType": "StateChange",
      "outcome": "Failed",
      "steps": [
        {
          "kind": "Given",
          "title": "Given 1 event(s)",
          "outcome": "Failed",
          "message": "One or more Given events do not exist on the slice.",
          "differences": [
            { "path": "CustomerRegistered", "expected": "an event on the slice", "actual": "not found" }
          ]
        }
      ],
      "note": "State change: verified the When command, the Given/Then events resolve to the slice, …"
    }
  ]
}
```

- **`outcome`** is `Passed`, `Failed`, or `Inconclusive` — the last meaning the step could not be verified for that
  slice type yet, which is not a failure.
- **`steps[].kind`** follows the specification's own shape (`Given`, `When`, `ThenEvents`, `ThenErrors`, …), so a
  failure points at the clause that disagreed rather than at the specification as a whole.
- **`differences`** carries the expected/actual pairs behind a failure, addressed by `path`.
- **`note`** states what was verified for that slice type — and, just as importantly, what was not.

:::note
Verification is **model-level**: the runner checks a specification against the model it belongs to — that the
events and commands it names resolve to the slice, and that the modeled rules and expected errors agree.
Executing the slice behaviorally against a live Chronicle is a follow-up, and each result's `note` says so.
:::

The types behind the file live in `Cratis.Stage.Contracts` (`SpecificationRunResults`,
`SpecificationRunResultsFile`), so tooling should deserialize with those rather than reading the JSON by hand.

## Opt in to semantic execution

A running Stage host with the semantic engine also exposes
[`POST /stage/semantic/specifications/run`](index.md#run-specifications-in-a-semantic-session). The host launches
this runner as a child process with `--engine semantic`, reads and validates its output file, and returns the
same `stage-spec-run/1` report. Both paths create fresh isolated state per specification; neither runs against
a live session's world or event store. Capability refusals remain typed `Unsupported` results with their details.
Runs remain available when an admitted model's live world rebuild or Chronicle registration fails later;
initial model compilation or runtime admission refusal instead returns HTTP 501.
The host route is additive and does not change either runner's `results.json` format or the default engine.

The semantic engine is opt-in in this minor version. Use `--engine semantic` for behavioral execution rather than the deprecated structural check:

```bash
dotnet run --project Source/SpecRunner -- \
    --engine semantic --model /path/to/Projects.play \
    --output /path/to/semantic-results.json \
    --application Projects
```

The output is **not** the legacy Guid-keyed `results.json`. Read it with
`SemanticSpecificationRunReportFile`: schema `stage-spec-run/1` contains semantic IDs, a revision, and
per-spec `Passed`, `Failed`, `Unsupported`, or `Cancelled` outcomes. An unsupported capability carries its
kind, the offending semantic construct ID and a reason. Do not feed this file to a legacy structural-results
reader. The process returns `0` after writing the report even when a specification failed or was blocked;
read its outcomes. The schema remains `stage-spec-run/1` when a reached described command handler reports
`Unsupported(ImplementationBody)`. Older readers reject the unknown `ImplementationBody` capability name;
update them before reading these reports. Authorization and validation refusals still execute before the body,
including reaction-invoked commands; the handler appends no facts.
Invalid semantic IDs, engine names, or structural-only flags with `--engine semantic` return `2`.
Missing or invalid catalog/model inputs return `1` with a diagnostic.

A specification case table produces one result per case, named `<Spec>_<Case>`. When a case fails,
each failure starts with `Case '<Case>' of '<Spec>':` and includes `Effective fixtures:` with the
substituted values and their origins, such as `when: name = "large" (case)`. Passing cases have no
failure text. The Host's semantic specification run endpoint returns the same case details.

The semantic engine runs admitted commands through Arc's in-memory command pipeline, with a fresh Chronicle
in-memory event log per specification. It supports explicit-source Given events and direct `when appended`
events, unconditional command production with literal or command-property mappings, and event assertions
(including any-order assertions). It evaluates portable command authorization before dispatch: `given caller`
sets an Arc principal, while authenticated, role, claim (literal, subject or artifact), and/or policies follow
Screenplay's reference semantics. Nested artifact paths cannot reach composite values, because composite command values are not admitted. `then denied` checks Unauthorized and appends no new facts. Roles and claims
remain distinct; a role-URI claim is blocked as `Unsupported(Authorization)` rather than treated as a role.

Given, appended and produced events can carry composite members and collections, including collections
of composites. Event materialization preserves missing members, nulls and empty collections as distinct
values; event assertions compare their structure rather than formatted object text. Child collections use
typed composite elements and ordered structural comparison, including missing optional scalar members.
Optional mapping sources and null literals within children remain refused because typed instances lose
the distinction between a missing member and an explicit null. Direct
structured property mappings remain refused. Product arithmetic remains a separate named refusal, even
when the same projection also has children.

Command rules (`NotEmpty`, `Minimum`, `Maximum`, `Equal`, `NotEqual`, `GreaterThan`,
`GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`, `Length`, `Matches`), scalar concept rules,
and command requirements reject on every failed severity (including warning and information). The first
rejection message follows reference order: command rules, concept rules, requirements; authored messages
and Screenplay's default messages are preserved. Collection-only rule kinds and non-scalar values remain
unsupported. Unique-value and unique-event constraints are checked before append, including prior Given
facts, releases, casing and replacement within a batch; a violation is an atomic rejection with the
constraint name in `trace.rejectionCode` and the reference message in `trace.rejection`. Direct append enforces the same constraints but
does not run command authorization or validation. Conflict outcomes cannot be reached from these admitted
specifications: the Screenplay v4.24.0 reference evaluator declares `SemanticConflict` but never produces it.

Facts are compared against Stage's recorded occurrences; Chronicle's persisted log is checked for fact count.
A separate Chronicle projection scenario evaluates admitted read-model assertions, including keyed absence
assertions (`then no readmodel … for …`), and optional snapshot queries by the read-model identifier.
An absence assertion passes only when that keyed instance is missing; an instance at another key does not
make it fail. Specifications can inspect projected Given events without a command or append action.
Protected queries evaluate the same portable policies as commands, with the fixture caller, query argument
and query key as the subject. An allowed query compares its result; a denied query matches only `then denied`,
not an empty result or `then error`. A reached opaque policy returns `Unsupported(Authorization)`. Flat scalar projections and scoped projections using
event-source keys (text or UUID) support scalar `set` mappings from event properties, event-source identities or typed literals, plus event-property `add`/`subtract` and numeric `increment`/`decrement`. Primitive numeric zero initialization with `add`/`subtract` is also supported; nonzero arithmetic literals, factors and expression-valued arithmetic remain refused. These mappings and property-keyed root removals run with per-specification artifacts. Event-source key types compare their underlying text or UUID primitive, so compatible concept and primitive types can share a projection. Keys must still satisfy the read-model identifier's enumeration values and canonical UUID form. Pinned generated command identifiers participate in this check; root `from` blocks can also use a required scalar event-property key of the identifier's value type or a non-empty text literal key for a text identifier. Optional numeric increment/decrement targets start from zero when unset. Given read-model seeds replace history at the resolved projection key, not the event-source key. The
semantic trace includes keyed read-model and query results; a mismatched result fails the specification. A named projection-lowering refusal is reported as `Unsupported(Projection)`; unexpected projection runtime failures fail the specification with the exception message. Neither is treated as a command rejection. For specifications expecting an error or denial, projections never run: actual rejections are compared, while accepted commands report the outcome mismatch before projection execution. Their produced-event projections are therefore not admission-relevant. Admission and runtime share the same projection-observation predicate: explicit root event blocks, removals, flat transitions and all-event subscriptions. Unrelated projections are not admitted merely because a specification reaches some other event; explicitly seeded or asserted read models and queried results still require admission.
DateTime read-model properties remain `Unsupported(Projection)`, with the property named in the refusal.
The reference compares DateTime text ordinally, not by instant. Materializing the value as `DateTimeOffset`
loses an authored `Z` suffix: the per-run projection returns `+00:00` instead. Numeric offsets and seven-digit
fractional precision can survive the scenario, but not every accepted round-trip spelling. Payload mappings
and clock-produced values therefore remain refused; Stage does not normalize either side to make assertions pass.
The clock is injectable through the in-process API. The tenant and identity allocator options are reserved
and do not affect this run; implicit identity allocation remains unsupported.

Given read models seed isolated state per read-model type and key. A seed replaces that key's projected Given
history; subsequent command, append and reaction facts fold on top through Chronicle 19.83's per-key scenario
seeding. Other keys retain their projected history, and invoked commands read the same seeded world. Seed-only
models can be asserted and queried without a projection. Projected seeds must contain every scalar property
and an identifier equal to their key; partial or null projected seeds remain `Unsupported(GivenReadModel)`
because concrete scenario records cannot preserve missing-property presence. Generated absence specifications
also seed complete scalar records for independent projections, preserving the same history replacement and
later command or append replay. They can assert absence beside presence at another seeded key or inspect
seeds without an action. Partial or null seeds, seeded decision reads, reducers, dependent projection scopes
and seeded query combinations retain `STAGE-ESM-011`.

Per-run projections reject optional or composite
read-model properties, DateTime read-model values (whose round-trip text may change), and non-identifier
properties whose generated name matches `Id` case-insensitively (including authored `id_`, `_id`, `id-`, `ID`, `iD`, and `i_d`, which collide with
Chronicle's document key); a child `id` remains allowed. Events and commands whose property names collide after PascalCase generation with another property, their record type, synthesized or inherited members, or generated command methods block every run, even when the event is not produced by the specification. The block is `Unsupported(Projection)` if a projection references that event; otherwise it is `Unsupported(Command)`. Concept and composite-type member or generated type-name collisions also block every run as `Unsupported(Command)`, even without a projection. Per-run projections also reject join removals, nested scopes, and unsupported key shapes before execution; identified child collections, identity joins and root joins run as described above. Projections over events whose property names Chronicle interprets as expressions (`$`, `.`, `[`, `]`, a leading quote, booleans, numbers, or derived functions such as `Week` or `Week()`), projections referencing even unreached events (including in child or nested scopes) with colliding generated members, and read-model properties whose names can be interpreted as paths or collide with another generated property, the record's type, or generated query methods return `Unsupported(Projection)`. Query method overloads that compile remain allowed; duplicate generated signatures and arguments named `readModels` do not.
An event-source identity
mapping into a non-identifier property requires exactly the identifier's type without enumeration constraints;
otherwise it returns `Unsupported(Projection)`. More than one projection for a requested read model also
returns `Unsupported(Projection)`. They also apply the renderer's scoped-projection admission predicate, so
composite keys and Chronicle#4125 shapes remain rejected. The per-run executor runs identity-only `every` mappings for the declared `from` event types and identity-only `all` (`FromAll`) mappings for all facts, creating an instance per event source. Every fact observed by `all` must have the identifier's value type and enumeration values. Literal `every` mappings apply to each declared `from` block and override an exact scalar `set` on the same target. Literal or empty `all` blocks and `every` including children remain unsupported. Queries other than optional snapshots
by identifier return `Unsupported(Query)`. Other unimplemented
behavior (implicit event-source identity allocation, external effects and
unsupported expression or value shapes) is also blocked rather than reported as a pass. The semantic
executor can run alongside a host without replacing its process-wide Arc service provider. This is not
a live Chronicle integration check. Keep the structural engine for existing Studio jobs until consumers
explicitly migrate to the new report schema.
