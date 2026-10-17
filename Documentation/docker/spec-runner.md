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
expectations, and matches any-order expectations by assignment at ESM v8 and later. Both engines refuse public-event
publication (`STAGE-ESM-032`), foreign events and `source events` captures (`STAGE-ESM-033`), and directed
Translate slices (`STAGE-ESM-024`, naming the direction), and reducer observer filters (`STAGE-ESM-035`).
The structural engine still refuses every reaction execution identity (`STAGE-ESM-034`) and reaction observer filter (`STAGE-ESM-035`); semantic renderer support for reaction filters and role-only or authenticated-only `runs as` invocations does not relax these structural refusals. Per-production route overrides fail `STAGE-ESM-036`
on the semantic engine; the structural engine refuses their stream syntax with `STAGE-ESM-030`.
The semantic executor settles event-triggered reactions after each command or direct append, including
`where`, productions, invocations, cascades, `runs as`, `from`, mixed effects and append constraints.
It processes new facts in append order, reactions in semantic-identity order and triggers in authored order,
producing events before invoking commands. Given events establish history without firing reactions.
An invocation uses the declared system roles, or no caller; it never inherits `given caller`, and system
claims remain unknown. Invoked generated commands require unavailable deterministic fixtures and return
`Unsupported` for `IdentityAllocation`. More than 1000 new facts returns `Unsupported` for `Reaction`.
A command specification with a millisecond-aligned `given clock` (no earlier than the Unix epoch) runs with that
instant as the command occurrence. Clock, application, startup and shutdown triggers, opaque bodies, `when clock`,
clocks on reaction specifications (and, in this executor, any clock in a model with reactions) and trigger/capture specifications remain refused with `STAGE-ESM-024`; unsupported `$context.causedBy.*` mappings retain `STAGE-ESM-013`.
A production mapping `$context.occurred` without `given clock` rejects the scenario instead of inventing time.
The executor uses its own reaction loop. Unlike the [semantic Stage Host](index.md#opt-in-to-semantic-execution),
which refuses the whole live batch if settlement fails, a specification ends at its first rejection or
unsupported capability. Rendered Chronicle reactors run eventually and retry failed partitions.
Structural refusals exit `1` without writing results;
semantic runs record `Unsupported`, never a passing specification. The semantic executor uses Screenplay 4.206.0, admits exact ESM v1–v27
version pairs (not mixed/noncanonical pairs) and refuses unaudited language/semantic versions (v28 and later) with `STAGE-ESM-016`.
A v22 model with `reaction batches atomic` is admitted and records `Unsupported` with `STAGE-ESM-043` until atomic batches execute.
At v27 a specification that projects into a read model with several `key` parts records `Unsupported(Projection)`,
naming ESM v27 composite read-model keys: the per-run scenario seeds and looks up instances by one event-source key.
The semantic Host runs such read models; the legacy structural runner refuses them with `STAGE-ESM-048`.
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
The semantic executor runs protected command reads, including reaction-invoked commands. After authorization and
property/concept rules, it projects the prior facts through Chronicle's in-memory read-model scenario and requires
each declared view at its key. Event-source-keyed removal and recreation are preserved. Absence rejects validation
before all requirements and generation, with `<View> '<key>' does not exist.` Read-backed requirements and
produced-event mappings use that projected state. Non-Error requirements on protected commands remain
`Unsupported` with `STAGE-ESM-045` until Screenplay defines their severity semantics. This admission check includes
commands reachable through reaction invocations, even in specifications expecting rejection, and runs before any facts are appended.
Join removals, parent keys, children and nested projection scopes remain refused.
The renderer renders protected reads and refuses only the shapes it cannot preserve with `STAGE-ESM-044`
(see [Protected reads rendering](../reference/protected-reads-rendering.md)). The structural engine refuses the preamble,
exits `1` and writes no results file.
Without the preamble, command reads still fail binding with `PLAY0271` and the repair diagnostic `PLAY0786`.
At v20 it executes rules on generated concepts. A failing fixture is a contract rejection with
`Generated fixture '<property>' fails concept '<Concept>' rules`, not the rule's authored input-validation message.
Each supplied fixture is checked for shape and rules before the next fixture or a missing fixture.
Generated concept rules render at every severity; the pinned Arc 22.112.0 runtime rejects any failing generated rule as a server generation defect.
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
unmatched keys and omitted optional filters. Populated equality-filtered query assertions retain
`STAGE-ESM-010`, and seeded read-model specifications retain the `GivenReadModel` gap.
The Host executes query filters and validation through the reference evaluator.
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

## Recovery and empty-effect specifications

At ESM v17, `when redelivered` selects an existing given occurrence and delivers it only to the named
reaction. The semantic executor applies that reaction's observer filter and `where`, without appending the
input again or delivering givens to sibling reactions. It then settles all observers of newly produced facts,
including invoked commands and downstream cascades. A nonzero given position selects that occurrence,
not the first event of the same contract. An unhandled invocation refusal can be asserted with `then error`.
Existing context, clock, generated-value and projection capability limits still apply; redelivery is not replay
or a guarantee of exactly-once external effects.

`then no events` asserts an empty new-fact set on a successful run. For commands it counts all command and
cascade facts. For redelivery it counts only new effects, not givens. For `when append` it excludes exactly the
initiating append and counts every following effect. It may accompany response, read-model or query
expectations, but not event, event-order, error or denial expectations. A nonempty result fails the specification;
omitting event expectations in an older model does not acquire this assertion.

This execution support is broader than generated specifications. Rendered redelivery supports one event
trigger on the named reaction: either direct productions or one command invocation supported by the existing
command harness. Stage pins Chronicle 19.71.0 and records every given through `ReactorScenario.History`,
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
constraint and projection checks. It then skips the remaining invocations of that trigger, not sibling
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
| `1` | The input path is missing, the file extension is unsupported, the folder is empty, model compilation fails, or a structural run refuses event-source routes (`STAGE-ESM-030`), public events (`STAGE-ESM-032`), foreign events or source-events captures (`STAGE-ESM-033`), directed Translate slices (`STAGE-ESM-024`), any reaction execution identity (`STAGE-ESM-034`, unchanged by renderer admission), or observer filters (`STAGE-ESM-035`, unchanged by semantic renderer support). Semantic runs refuse production routes with `STAGE-ESM-036` in their report. An actionable error is written to standard error; no results file is written. |
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
read its outcomes. Invalid semantic IDs, engine names, or structural-only flags with `--engine semantic` return `2`.
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
event-source keys (text or UUID) with scalar `from`/`set` mappings run with per-specification artifacts. The
semantic trace includes keyed read-model and query results; a mismatched result fails the specification.
The clock is injectable through the in-process API. The tenant and identity allocator options are reserved
and do not affect this run; implicit identity allocation remains unsupported.

Given read models still return `Unsupported(GivenReadModel)`. Per-run projections reject optional or composite
read-model properties, DateTime read-model values (whose round-trip text may change), and non-identifier
properties whose generated name matches `Id` case-insensitively (including authored `id_`, `_id`, `id-`, `ID`, `iD`, and `i_d`, which collide with
Chronicle's document key); a child `id` remains allowed. Events and commands whose property names collide after PascalCase generation with another property, their record type, synthesized or inherited members, or generated command methods block every run, even when the event is not produced by the specification. The block is `Unsupported(Projection)` if a projection references that event; otherwise it is `Unsupported(Command)`. Concept and composite-type member or generated type-name collisions also block every run as `Unsupported(Command)`, even without a projection. Per-run projections also reject joins, child or nested projections, removals, every/all mappings, literal mappings, and unsupported key shapes before execution. Projections over events whose property names Chronicle interprets as expressions (`$`, `.`, `[`, `]`, a leading quote, booleans, numbers, or derived functions such as `Week` or `Week()`), projections referencing even unreached events (including in child or nested scopes) with colliding generated members, and read-model properties whose names can be interpreted as paths or collide with another generated property, the record's type, or generated query methods return `Unsupported(Projection)`. Query method overloads that compile remain allowed; duplicate generated signatures and arguments named `readModels` do not.
An event-source identity
mapping into a non-identifier property requires exactly the identifier's type without enumeration constraints;
otherwise it returns `Unsupported(Projection)`. More than one projection for a requested read model also
returns `Unsupported(Projection)`. They also apply the renderer's scoped-projection admission predicate, so
composite keys and Chronicle#4125 shapes remain rejected. The per-run executor runs identity-only `every` mappings for the declared `from` event types and identity-only `all` (`FromAll`) mappings for all facts, creating an instance per event source. Every fact observed by `all` must have the identifier's value type and enumeration values. Literal `every` mappings, literal or empty `all` blocks, and `every` including children remain unsupported. Queries other than optional snapshots
by identifier return `Unsupported(Query)`. Other unimplemented
behavior (implicit event-source identity allocation, external effects and
unsupported expression or value shapes) is also blocked rather than reported as a pass. The semantic
executor can run alongside a host without replacing its process-wide Arc service provider. This is not
a live Chronicle integration check. Keep the structural engine for existing Studio jobs until consumers
explicitly migrate to the new report schema.
