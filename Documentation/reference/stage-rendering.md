---
title: How the Stage renders a model
description: How the running Stage draws a model's screens, screen templates and layouts through the default blueprint.
---

The Stage draws the model it was started with. It does not generate screens for it and it does not own a shell of
its own: it renders the scene the model describes through the default blueprint
(`@cratis/scene.blueprint.default`), the same application shell, chrome, themes and components a Scene
application gets.

## What is drawn

| Part of the model | Rendered as |
| --- | --- |
| `screen` | One entry in the blueprint menu, addressed by `#/<ScreenName>`. |
| `screen template` | The template's `arrangement flow` positions the screen's slots, evaluated for the current size class. |
| `layout` | The blueprint's shell for that layout name (`AppShell`, `FullPage`). A layout the blueprint does not provide is drawn in `AppShell`. |
| `ui profile` | Decides which packages a bare component name resolves against. `core`, `Cratis.Components` (React Aria) and the blueprint are always admitted. React Aria is the zero-configuration default; `packages PrimeReact` explicitly opts in to PrimeReact. Its `root template`, shell options and `appearance` decide the shell the application starts in (see [Root template, options and appearance](#root-template-options-and-appearance)). |
| Command and query | Components wired to the routes the running application exposes. |

With `packages PrimeReact`, the frontend adds the PrimeReact packages and icons, its provider, theme CSS, styling plugin and build-time PrimeUI license. Without that declaration, the scaffold emits none of them and the live Stage does not load the provider, license or theme. Bare names shared with React Aria resolve to PrimeReact only when it is listed; blueprint names still take precedence.

A bare component name in a template (for example `title` or `inputText`) is resolved against the profile's
packages before it is rendered, so a template written against the blueprint's vocabulary draws real components
rather than placeholders. A name no package declares renders a placeholder that names what is missing.

## Guarded actions and interactions

A simple `action … execute <Command>` translates to `core:action`. A guarded action with ordered
`when … execute` alternatives and an `otherwise` also translates to `core:action`, carrying its alternatives,
their guards and the fallback. The Stage frontend runtime (the `@cratis/stage.runtime` package), which a live Stage
and a generated React application both render with, evaluates the guard against the item the screen's nearest `data` declaration reads:

- The first alternative whose guard holds is the command the action executes, with its `with` values read from
  the item. A missing or null field does not match.
- When no alternative holds, `otherwise hidden` renders nothing, and `otherwise execute <Command>` offers that
  command instead.
- Without an item, the action is not offered at all, even if it has a fallback command.
- The guard is evaluated again whenever the item changes, so the action follows the data.

Guarded `on click`, `on double click`, and `on select` interactions translate to one Scene interaction binding
per branch, in authored order. The Scene interaction engine runs exactly the branch that holds for the row the
interaction happened on, or the `otherwise` actions when none does, and runs nothing without a row. A table that
declares a double click delays its single-row click briefly, so a double click on a row does not first select or
navigate.

### Guards Stage cannot evaluate

Stage evaluates guards that compare `item.<field>` with a literal using `==`, `!=`, `>`, `>=`, `<`, `<=`,
`contains` or `startswith`, combined with `and` and `or`. Any other guard, and any fallback other than `hidden`
or an `execute` that names a command, is refused rather than guessed at: runnable application rendering and
live Host serving refuse a guarded action with `STAGE-SCENE-ACTION-001` and a guarded interaction with
`STAGE-SCENE-INTERACTION-001`, identifying it and its source location when source is available. This also
applies inside sections and template slots, and to guarded actions supplied as Scene JSON. The Host records
these refusals as unsupported capabilities: `/stage/status` reports `unsupported`, and `/api` requests return
HTTP 501 rather than executing an unconditional action. If such a guard reaches the frontend runtime anyway, the
action is shown disabled with the diagnostic and never executes.

## Conversion diagnostics

Importers can retain the supported parts of a model without treating an unsupported projection or arrangement
as an exception. `ScreenplayEventModelVisitor.Convert(ApplicationSyntax)` returns an `EventModelConversion`
with `Model` and `Diagnostics`. The additive `ProjectionConverter`, `ReadModelConverter`, `SliceConverter`
and `ArrangementConverter` overloads accept a `ConversionDiagnostics` collector. Each diagnostic carries
`Code`, `Severity`, `Construct`, `Path`, `Location` and `Message`; `ToDiagnostic()` converts it to a Screenplay
diagnostic. `All` preserves reporting order, and `HasErrors` distinguishes errors from warnings.

An unsupported projection is omitted as a whole, not partially translated. Its read model remains, with
`Projection = null` and `UnconvertedProjectionName` naming the omission. Other slices, commands, events and
specifications remain available. Additional projections receive a warning because a Stage read-model definition
carries only the first projection. Existing converter overloads keep their throwing behavior;
`ScreenplayEventModelVisitor.Visit` stops at the first unsupported projection with the original
`InvalidEventModel` from `SliceConverter`, preserving its message.
Other legacy engine admission checks still apply to both visitor entry points.

`ScreenplaySceneVisitor.Convert` records arrangement conversion diagnostics in
`SceneApplication.ConversionDiagnostics`. The existing `Visit` entry point uses the original throwing path,
stopping at the first invalid arrangement with `UnknownArrangementNode`, `UnknownArrangementContainerKind`,
`UnknownSizeClassName` or `FormatException` as appropriate. Unsupported nodes and containers lose
their subtree; invalid sizes omit the affected placement, override or variant. Supported siblings remain. These diagnostics are not Scene
JSON members, so keep them alongside the translated Scene when importing. The Cratis renderer refuses Error
conversion diagnostics before creating a profile; warnings alone do not refuse it.

| Code | Severity | Construct |
| --- | --- | --- |
| `STAGE-PROJECTION-001` | Error | Projection sequence |
| `STAGE-PROJECTION-002` | Error | Projection variant |
| `STAGE-PROJECTION-003` | Error | Unsupported projection block |
| `STAGE-PROJECTION-004` | Error | Unsupported projection mapping, including its property |
| `STAGE-PROJECTION-005` | Error | Unsupported projection expression |
| `STAGE-PROJECTION-006` | Error | Unsupported projection key |
| `STAGE-PROJECTION-007` | Error | Unsupported automap mode |
| `STAGE-PROJECTION-008` | Warning | Additional projection not converted |
| `STAGE-SCENE-ARRANGEMENT-001` | Error | Unsupported arrangement node |
| `STAGE-SCENE-ARRANGEMENT-002` | Error | Unsupported container kind |
| `STAGE-SCENE-ARRANGEMENT-003` | Render finding | Unsupported Scene arrangement type |
| `STAGE-SCENE-ARRANGEMENT-004` | Error | Invalid placement size or unknown size class |

`ArrangementSelector.TrySelect` returns `false` for an unsupported arrangement type. `RenderPlanner` reports
`ArrangementUnsupported` (value `20`) with `STAGE-SCENE-ARRANGEMENT-003` instead of throwing. A known freeform
arrangement missing the target size-class variant remains a separate missing-variant finding. Package catalog
failures continue to use the existing render findings.

## Generated C# self-reducing read models

Artifact planning can opt a confirmed new application into self-reducing read models.
Existing callers remain classic; this does not change the running Stage's semantic
execution or automatically change CLI and Studio output. See
[the profile and persisted configuration contract](plan-from-sources.md#reducer-rendering-convention).

In self mode, the read-model record in the existing slice file contains one public
static `Reduce` overload per observed event and a non-public static typed-context
helper for each verified pure transition. Its queries remain unchanged. Both
StateView and Automation slices use this layout; no `IReducerFor<T>` companion is emitted.
Observer source and stream filters are attributes on the model. Tenant translation,
occurrence time, checked sequence conversion, immutable inputs, deletion, and exact
event-source identity checks retain the classic semantics. The scaffold takes the exact Chronicle
and Arc pins for this generated surface from Stage's `Directory.Packages.props` at build time.

Self-mode admission checks the same record and query signatures used for rendering,
so model properties and query names participate in pure-body binding. Rendering and
analysis share the entire file's imports and visible declarations, including sibling
reducers, projections, and Automation commands. Observable query signatures are
represented in closed analysis without allowing query or reactive-service calls from
pure transitions. `Reduce` as a
model, property, or query name, the replay-hook query names `BeginReplay`, `EndReplay`,
`BeginReplayPartition`, `EndReplayPartition`, and collisions with generated
`Transition_<suffix>` members refuse with `STAGE-ESM-019`. Rename the conflicting
member; Stage never silently switches a self model back to classic.
Existing purity and typed-context refusals still apply.

For the Chronicle runtime contract, see
[self-reducing read models](/chronicle/reducers/self-reducing-read-models/) and
[choosing a read-model style](/chronicle/projections/choosing-a-read-model-style/).
Existing applications migrate one read model at a time: the self-reducing model keeps
the classic reducer's observer identity and the plan requires removing the old file.
See [migrating a classic read model](plan-from-sources.md#migrating-a-classic-read-model).

## Frontend query execution

Stage tables and guarded actions resolve the modeled query name to a registered Arc query proxy.
A route only disambiguates proxies sharing that name; an unregistered query is not fetched directly.
Generated applications register their generated proxies. The live Stage host registers native Arc
query proxies from its discovered routes before rendering.

Snapshot queries use Arc's `perform` with the configured origin, API base path, microservice and
HTTP identity headers. Route parameters are substituted by Arc; remaining arguments go in the
query string for GET queries. Queries wait until their required argument bindings are ready.
Observable queries use Arc's configured subscription transport (including the native hub for
generated proxies with their fully qualified query names). Only the live Stage host configures its
Arc provider with `queryDirectMode`, preserving its direct per-route transport because its proxies
have routes rather than fully qualified backend query identities. Queries unsubscribe when their
view or arguments change. The SSE EventSource relies on the browser's cookie policy, not the HTTP headers callback.
If no first ready subscription result arrives within 15 seconds, Stage unsubscribes and performs
one read through Arc to surface a rejection rather than leave the view loading indefinitely.
After the first ready result, Arc owns reconnection.

The generated-application build harness packs the runtime after running `npm ci` in
`Source/Frontend`, resetting that checkout's `node_modules`. Do not run frontend checks
concurrently with that harness in the same checkout.

## Inline command handlers

For legacy syntax rendering, an inline C# command handler keeps its authored body and receives
`CommandContext context`. Its return type comes from the body, not from declared `produces` clauses:

| Authored body | Generated return type |
| --- | --- |
| Any `yield return` or `yield break` | `IEnumerable<object>` |
| No `return <expression>` (no returns, or only `return;`) | `void` |
| Every `return <expression>` constructs the same explicit type with `new T(...)`, and its rightmost name is non-generic | `T` |
| Anything else, including a generic constructed type, returned variable, array, mixed types or target-typed `new(...)` | `IEnumerable<object>` |

A body mixing `return;` with `return <expression>;` is rejected with `MixedReturns` under
`STAGE-CRATIS-INLINE-001`: return a value or nothing on every path. Bare returns alone still render as `void`.
Returns and yields inside lambdas, anonymous methods and local functions do not affect the signature or this check.
Explicit type names are compared after whitespace normalization; qualified names in the authored body, such as
`global::Cratis.Chronicle.EventSequences.EventForEventSourceId`, are preserved. A constructed type whose
rightmost name is generic, such as `List<object>` or `System.Collections.Generic.List<object>`, keeps
`IEnumerable<object>` so collections retain the collection-of-events signature. Returning a list variable
also keeps `IEnumerable<object>`. A single constructed type is preserved even when it is hand-written:
unless it names a model-declared event or `EventForEventSourceId` (qualified or unqualified), rendering
emits a non-fatal warning that the inferred type is unverified. If that type is a collection not implementing
`IEnumerable<object>`, its events are not appended; return the events as `IEnumerable<object>` or a single event.
This inference does not add async handler support or change file-backed handlers.

For example, assuming the slice declares an `OrderPlaced` event with no properties, this handler body:

```csharp
return new OrderPlaced();
```

renders as this method inside the generated command record:

```csharp
public OrderPlaced Handle(CommandContext context)
{
    return new OrderPlaced();
}
```

Admission uses the same inferred signature. It still refuses non-C# bodies, directives, malformed bodies
and bodies that depend on the generated `context` parameter with `STAGE-CRATIS-INLINE-001`.
Inference falls back to `IEnumerable<object>` for malformed bodies; admission rejects them rather than
rendering invalid code. Semantic implementation admission is unchanged.

## Generated C# query arguments

Query arguments retain their concept types so Arc validates the `by` key and supplied filter
values before invoking the generated method. An out-of-rule value returns HTTP 400 with validation
results; an in-rule key that matches nothing still returns `null` or an empty collection.
Validation also reaches concept members inside composite arguments and collection elements.
Authorization runs before validation. TypeScript proxy argument types are unchanged.
Nullable keys and optional concept filters with `not empty` rules receive `STAGE-ESM-010`:
Arc skips null concept values, while the reference validates nullable concept keys and rejects
explicitly null filters. An omitted optional filter remains distinct from a supplied null.

A query a `performer` answers is an opaque read: the executable model leaves it out of the slice's queries and carries its shape - the read model, cardinality, delivery, `by` key and filters, and who may call it (`SemanticPerformedQueryShape.Authorization`, Screenplay 4.236) - on the `QueryPerformer` implementation requirement. Stage renders it as the query that shape states: the read model record, the query method with its route, parameters (filters in declaration order, optional ones defaulted) and the `[Authorize]` policy the model names, and a read model such a query returns needs no projection or reducer. When a verified inline `csharp` or file body is attached, Stage embeds it in a compile-checked helper. The body returns a read-model instance (or null) for a single query, or `IEnumerable<ReadModel>` for a collection. `readModels` is the current read-model collection; caller arguments are available both as their camelCase parameter names and as PascalCase properties on `context.Arguments`. The application-local context also supplies `Tenant` (string), `Identity` (`ClaimsPrincipal`), and `ReceivedAt` (`DateTimeOffset` captured by Arc at query receipt). It deliberately has no command causation, `CausedBy`, or event `Occurred` member. Snapshot queries load instances once. The body is synchronous C#, not a pure reducer sandbox. Observable/live performers have no body-owned stream and render as explicit gaps rather than applying a body to read-model emissions. A non-C# body, missing attachment, changed or hash-invalid body, or body compile error also becomes a gap. Both planning and direct shape rendering produce the same compilable method boundary, throwing `NotImplementedException` with `Query '<name>' (requirement <id>) is not runnable: <reason>`. Planning reports one `STAGE-IMPL-006` Warning per such query ("performer rendered as a gap"), preserving the original reason, compiler diagnostic and body source location when available. The generated artifacts remain available.

Each gap has `ArtifactImplementationGapOrigin.Unverified`, an additive public contract member with minor-version intent, the compiler requirement identity, and the generated stub's signature, body locator and unrealized typed-context metadata. `STAGE-NOT-RUNNABLE.md` lists every gap in the plan. Any non-empty `ImplementationGaps` makes `Outcome` `Partial`; `Success` still means no Error diagnostics, so a partial gap plan can be successful without being fully runnable. Verified synchronous bodies remain unchanged, with `Outcome` `Complete` and no marker when no gaps exist. `STAGE-ESM-019`, `STAGE-ESM-020` and `STAGE-IMPL-005` keep their Error meanings for cases that still refuse; they are not reused as warnings. The semantic runtime continues to list the read model but not the opaque query. A policy implemented by code, a composite `by` key, an unresolved argument type, an observable single result or a required argument after an optional one refuse the read model with `STAGE-ESM-007`, naming the query and why. Explicit `scoped to` clauses remain refused by Screenplay (`PLAY0268`); Stage does not silently drop them.

ESM v18 adds query-only `then error` specifications. For empty fixtures, Stage generates an Arc
`QueryScenario` call that asserts validation failure and the expected message, when supplied.
These empty-world specifications also support `then no result`, but do not relax application
admission: every read model still needs a projection or reducer (`STAGE-ESM-007`), unless a performer answers a query for it. The
projection-less `QueryValidation/v18` corpus runs in the reference and Stage executors but is
refused as a rendered application; generated conformance specifications use a projected view.
Query rejection specifications with given events or read-model state are refused with
`STAGE-ESM-011`; the generated rejection scenario supports only empty fixtures. Sparse composite
query arguments also receive `STAGE-ESM-011`: supply every member, including optional members,
so generated literals do not invent values that change validation.

:::caution[Renaming events]
An event's Chronicle id is its stored name: the `id` pin, otherwise the name. Its generated class follows the current name. ESM v16 carries a differing pin as `storedName`, rendered as `[EventType("Original")]`; unpinned events retain argument-free `[EventType]`.

Before writing, publishers must call `CratisRendering.CheckPublication` at every scope and refuse every non-`Compatible` result. Only these publishers enforce `STAGE-EVENT-002` for changed ids. Existing `cratis render` versions check only scoped plans and refuse only `RequiresApplicationScope`; they do not enforce this diagnostic. The CLI integration is tracked in [Cratis/cli#354](https://github.com/Cratis/cli/issues/354). Pin the existing id in the model, add a new event instead of renaming, or write an event type migration. If the release baseline records the wrong name, correct it per [Screenplay decision 0070](https://github.com/Cratis/Screenplay/blob/v4.154.0/decisions/0070-derive-event-mutability-from-an-admitted-release-baseline.md).

A rename without a pin still orphans stored events: a renderer cannot distinguish it from deleting one type and adding another. Use the Screenplay workspace/MCP rename, which pins by default.
:::

## Render around refused specifications

Stage renders the application while omitting supported specification gaps by default.
When **every** Error is `STAGE-ESM-011`, `STAGE-ESM-024`, or `STAGE-ESM-051` and names a
specification in the model, Stage changes those diagnostics to warnings and skips only
those specification files. Each warning names the specification, the original diagnostic
code and the reason it cannot render. No opt-in is needed, including when
`CratisPlanOptions.Refusals` is `ArtifactRefusalMode.Refuse`.

Stage never omits commands, events, policies, or other application contracts. Any other
error still refuses the entire plan, including evolved events (`STAGE-ESM-026`),
implementation-envelope errors, and `STAGE-ESM-024` attached to a reaction, trigger,
capture, or slice. Specification admission guards, including the constraint-target guard,
remain unchanged; skipping a specification does not prove that it passes.

A plan with only skipped specifications has `Success == true` and `Outcome == Complete`.
Inspect `Refusals` on `ArtifactRenderPlan` or `CratisPlanResult` for the omissions. Each
refusal lists its semantic `Artifact`, `Kind` (`specification`), and sorted diagnostic
`Codes`; entries are sorted by artifact identity. No emitted artifact's `Sources` contains
an omitted identity, and no placeholder specification is emitted.

| Diagnostic | Severity | Meaning |
| --- | --- | --- |
| `STAGE-PLAN-007` | Information | Rendered around N refused specifications; inspect their warnings. |

Publishers still perform the ordinary publication and ownership checks. This behavior
does not change Host or specification-executor admission, and does not add a CLI flag.
The existing refusal-mode profile input remains accepted and hashed with the profile.
The output digest adds a `refusals-v1` segment only when `Refusals` is nonempty.

## Strict plan acceptance

Set `CratisPlanOptions.Strict` for source planning, or `CratisRenderingOptions.Strict` when
creating a direct rendering profile, to reject validated degradation at acceptance time.
The setting is recorded as the hashed `stage:strict` profile input, version `1`, with
exact UTF-8 bytes `true` or `false` and no trailing newline. An absent input means false;
default profile creation omits it to preserve legacy profile bytes. The shared
`Cratis.Stage.Contracts.Rendering.StrictAcceptanceInput` creates and reads this input.

Unverified query performers retain their generated throwing bodies and implementation-gap
metadata. Default planning preserves the existing `STAGE-IMPL-006` diagnostics and does not
add public degradation warnings or acceptance metadata. Default plans with unclassified
implementation gaps retain their legacy partial outcome and omit the unavailable inventory
and completeness reports rather than claim those owners are rendered. Only strict planning records
`STAGE-DEGRADE-002` warning evidence in `ArtifactRenderPlan.Degradations` and promotes that
recorded warning to an Error, so `Success` becomes false while `Outcome` remains `Degraded` and
`CanPublishArtifacts` remains true. Genuine generation or integrity Errors still prevent
safe publication. Strict acceptance is not a request to discard safe artifacts or reports;
planning itself never publishes files.

`WithAcceptance(plan.Degradations, strict: false)` restores the original warning evidence.
Release-baseline warnings and source diagnostics retain that evidence across plan
reconstruction. `WithInventory` accepts a validated construct inventory; its locations carry
the exact typed `SemanticAddress` as `semanticLocator` and a real portable relative
`modelPath`, or explicit null when no source filename is available. Semantic locators are
not filenames and must not be flattened into fabricated paths.

The current provider counts selected identity-bearing commands, produced events, read models,
queries, projections, reactions, captures, specifications, concepts and composite types. It does
not claim a complete inventory of every public construct kind: reducers and constraints lack
independent identities in the current semantic model, and policies, guards, validations and
screens are not yet independently inventoried. It never invents identities for those owners.
The shared contract supports their kinds for providers with explicit semantic ownership.
A refused construct currently requires refusal reasons and no degradation entries; independent
refused and degraded constructs may coexist in one inventory, with refusal taking precedence.

The shared `STAGE-DEGRADE-001` through `005` codes describe unknown members, unsupported
constructs, inline mismatches, a newer ESM inside an explicitly declared supported range, and
dependency degradation. Their availability is not a claim that the Cratis provider emits all
five today.

### A newer ESM inside the declared range

The Cratis renderer declares the range `>=32.0 <33`. The exact audited pairs are unchanged. A
language/semantic pair that is not audited, matches in major and minor, and lies inside that range
(for example 32.1) renders as **Degraded** with plan-level `STAGE-DEGRADE-004` evidence in
`ArtifactRenderPlan.PlanEvidence`, in the default and the strict mode alike (strict promotes the
warning to an error, as for any degradation, but the plan still publishes). Members that this
Stage version has not audited on selected owners add `STAGE-DEGRADE-001` evidence. A member that
carries security meaning (policies, authorization, claims and similar masks) is never rendered
around: the render is refused with `STAGE-ESM-016` naming the member. A future major (33 or later),
an out-of-range pair, a malformed pair and an unknown enumeration value are refused with
`STAGE-ESM-016`; the runtime and specification executor still support only the exact audited pairs.

Plan-level evidence names no selected construct, so it is excluded from the inventory, the
completeness score and the construct gaps. It does change the plan digest and the outcome.

When a completeness report is written for such a plan, it is the additive `stage-render-completeness/v2`
shape: `support` carries the `stage-render-audited-support/v2` version, the
`AuditedOrNewerWithinDeclaredRange` policy, the typed `declaredRange` (minimum pair and exclusive
major) and the `effective` policy and versions, and a `planEvidence` array lists the evidence.
Reports without a declared range keep the exact `v1` bytes and the `AuditedOnly` policy.

## Release-baseline warnings in artifact plans

An admitted release baseline freezes stored event revisions, including the resolved
concepts and types they use. The Cratis artifact planner reports changed and missing
admitted revisions as warnings when its caller supplies
`ArtifactRenderRequest.ReleaseBaseline`. It also warns when that baseline is
unreadable or the check is unavailable or stale. Warnings do not refuse rendering
or change generated artifacts; ordinary blocking diagnostics still refuse.

Source-based `PlanFrom` reads the model root's `.screenplay/release-baseline.json`
without writing it. In-memory callers can use `StageReleaseBaseline.Check` with an
authoritative workspace or compilation and Screenplay's baseline read. The assessment
covers the whole application, even for a scoped plan. Bare-ESM callers that omit it
remain unchecked; the live Host does not enforce these artifact-plan warnings.
See [Planning admitted release revisions](plan-from-sources.md#admitted-release-revisions)
for status and diagnostic codes.

A release step explicitly admits the baseline; rendering never does. Run
`screenplay baseline check <root>` in CI to gate edits. Its resolved-schema equality
is not Chronicle's schema compatibility relation. The destination check remains
separate: baseline warnings do not bypass `CheckPublication` or `STAGE-EVENT-002`.

The live semantic sandbox treats translation integrations differently from generated applications:
`STAGE-ESM-032` public events and event projections, `STAGE-ESM-033` foreign events and event-source
captures, and `STAGE-ESM-024` Translate slices and captures appear as `renderedAround` admission entries.
Public events are published to the application's outbox; inbound translation runs on seeded foreign facts and on what reaches the application's inbox, and never polls
external sources. These entries do not block unrelated local execution. Non-event reactions and other
unsupported behavior still refuse admission. This does not relax renderer or legacy-engine admission;
see [semantic sandbox execution](../docker/index.md#opt-in-to-semantic-execution). Generated applications
can instead render the supported inbox capture and outbox publication subset; see
[Translation rendering](translation-rendering.md) for its mappings, added-state approximation and refusals.

Systems, operation declarations and phases, inline `produces operation`, `given operation … fails`,
`then operation` and `then compensated` are syntax-only authoring constructs. The legacy renderer, Host
and structural specification runner refuse them with `STAGE-ESM-057`, naming the construct and its
source location, rather than dropping it or appending an event instead. The semantic path refuses the
same constructs with Screenplay's `PLAY0268`; operations are not admitted by any supported executable
model (ESM) version yet (Cratis/Stage#176, Cratis/Screenplay#301).

## Explicit action subjects

A toolbar item or screen action with an explicit `for item` or `for screen` subject refuses
with `STAGE-ESM-058` before any output is written. The diagnostic names the construct and
its source location: explicit action subjects are not yet rendered
([Screenplay decision 0110](https://github.com/Cratis/Screenplay/blob/v4.250.0/decisions/0110-explicit-action-subjects-and-selection.md),
[Cratis/Stage#855](https://github.com/Cratis/Stage/issues/855)). Omitting the subject keeps
existing output unchanged.

## Generated C# whole numbers

The semantic Cratis renderer maps whole-number primitives and concepts to `long` for ESM v8 and later,
and to `int` for ESM v1–v7. Older models with authored whole-number literals outside the `int` range
fail admission with `STAGE-ESM-031` rather than generating uncompilable C#.

Stage uses Screenplay 4.247.3 and admits exact ESM v1–v30 and v32 language/semantic pairs, including v11's corrected Double-mode literal lowering, v15's generation strategies with atomic freshness scopes, and v20's rules on generated concepts. V21's `generate pattern` and `generate random` render Fundamentals factories and execute with reference-compatible fixture shapes; dated patterns take their UTC date from the generation context, and random values preserve the full inclusive Int64 range. The specification executor runs pinned String `generate rule` values without invoking their bodies and still checks concept rules; missing pins remain `Unsupported(IdentityAllocation)`. The Host refuses commands that generate rule values with `STAGE-ESM-028`, naming the command, property, concept and rule, by design because the interpreter never runs implementation bodies; unused rule concepts do not block the Host or executor. The semantic renderer emits verified C# rule bodies through `IGeneratableInContext<T>.New(GenerationContext context)`, compile-checked against embedded BCL and pinned Fundamentals metadata references without the pure-body walk. Fundamentals is an embedded build-only reference, not a renderer runtime or package dependency. Missing attachments fail `STAGE-ESM-020`, body SHA-256 mismatches against `ContentHash` fail `STAGE-ESM-056`, body compile errors and warnings fail `STAGE-ESM-019` at the attachment, and shadowing fails `STAGE-ESM-022`. Any concept rule requires a pin for rule generation; concepts with no validation rules may generate unpinned in rendered specifications. The legacy syntax engine still refuses `generate rule` with `STAGE-ESM-028`. `validate checksum luhn`, `mod11` and `mod97` render and execute. The legacy syntax renderer still refuses checksum and unknown validation rule kinds with `STAGE-ESM-042`. Generated values with any unproven rule require pins in rendered specifications; fully proven rules permit unpinned generation. Failing pins retain `STAGE-ESM-011`, and unpinned specifications with unproven rules retain `STAGE-ESM-038`. The generated backend uses Stage's central Arc pin, which rejects generated values failing concept rules of any severity. Runtime generation defects produce exceptions rather than input validation failures. Applications using only implicit UUID generation remain unguarded even at later schema versions. See [Generated values](generated-values-rendering.md) for factory shapes, fixtures and timing limits.
Mixed version pairs, unaudited v31, and versions after v32 are refused with `STAGE-ESM-016`; only the specification executor runs v33 and v34 models (see [A specification runs its own slice only](../docker/spec-runner.md#a-specification-runs-its-own-slice-only)). V32 preserves declared capture event-source identities for required scalar text, UUID and whole-number keys, including non-enumerated concepts over them. Inconsistent or non-convertible identity types fail with `STAGE-ESM-052`, never an implicit text fallback; see [Typed capture keys](translation-rendering.md#typed-capture-keys-esm-v32). The semantic Host and specification executor run protected command reads, read-backed requirements and produced-event mappings. The Cratis renderer renders `command reads protected`, command `reads`, read-backed requirements and read-path mappings as Arc read protection; see [Protected reads rendering](protected-reads-rendering.md). It still refuses the shapes it cannot preserve with `STAGE-ESM-044`, each naming its reason: a read path in a `produces when` condition or destination, a read-path mapping to an optional or differently typed event property, and reads combined with generated-value freshness scopes. Non-Error requirements on protected commands, including reaction-invoked commands, remain refused before execution with `STAGE-ESM-045` until Screenplay defines their severity semantics. See the [semantic Host](../docker/index.md#opt-in-to-semantic-execution) for how it commits decisions against outside appends. The legacy renderer, Host and structural specification runner also refuse the preamble with `STAGE-ESM-044`; without it, reads still fail binding with `PLAY0271` and the repair diagnostic `PLAY0786`. ESM v24's `contains`, `not contains`, `starts with` and `ends with` rules execute on the Host and specification executor and render as ordinal UTF-16 string comparisons, without regex or a Screenplay dependency in generated apps. Absence passes; an empty operand satisfies contains/starts-with/ends-with and fails not-contains for every present value. In admitted v22+ models, the semantic renderer supports `reaction batches atomic` when each generated event handler has at most one direct-production trigger: its returned event collection appends as one batch, and each selected invocation refusal branch returns its own event collection. Multiple same-event production triggers in one reaction retain `STAGE-ESM-043`, naming the unsupported shape. Screenplay rejects routes on direct and refusal-branch productions before Stage admission; Stage retains a defensive `STAGE-ESM-043` guard for routed direct productions. Atomic stream closing has no Screenplay ESM representation yet and is not rendered (Screenplay#770). Other reaction admission limits still apply, including `STAGE-ESM-037` for mixed events and invocations. The legacy syntax renderer still refuses the atomic declaration. The semantic Host and specification executor execute each batch all-or-none. See [Atomic batch rendering](reaction-rendering.md#atomic-batch-rendering) and the executor's [Atomic reaction batches](../docker/spec-runner.md#atomic-reaction-batches). The renderer refuses described command handlers with `STAGE-IMPL-002`, naming the command and RequirementId. No stubs are emitted yet, even in Stub mode.

Several same-event production triggers in one atomic reaction stay refused with `STAGE-ESM-043` until Chronicle can return ordered per-trigger append batches from one reactor handler ([Chronicle#5248](https://github.com/Cratis/Chronicle/issues/5248)). A reactor appends what it returns as one batch, so rendering the triggers into one handler would merge their batch boundaries.

The semantic Host and specification executor admit models containing described command handlers. Authorization,
input validation, declarative reads and requirements run before the body; denied and invalid requests keep their
normal outcomes. Only a reached handler body reports `Unsupported(ImplementationBody)`, including a
reaction-invoked handler. Its declarative `produces` clauses are not executed and no handler facts are appended.
An unsupported body cannot select an invocation refusal branch. The Host lists the handler command as unsupported
without blocking the live model. Executor details use `STAGE-IMPL-002:`; live Host details retain Screenplay's
message without that prefix. Both name the command and RequirementId.

The `stage-spec-run/1` report schema is unchanged, but its capability names now include `ImplementationBody`.
An older reader rejects that unknown enum name; update report readers before consuming these results.

The semantic Host and specification executor never execute implementation bodies. They report
`STAGE-IMPL-003` only where a reducer transition, command validation or concept validation body is reached,
naming its owner and RequirementId. Unrelated commands, queries and specifications remain admitted.
Command input types and query parameter types reach concept rules transitively through composite types,
collections and optional values. A reducer is reached by events introduced by a specification or command,
including reaction cascades, or by an assertion or query on its read model. The reach check conservatively
includes possible reaction effects without evaluating runtime guards. Reducer refusals carry the Projection
capability; validation refusals carry Command. This does not change live-world history rebuilding or the
renderer's implementation-body admission.

Pure reducer bodies may use Stage's generated primitive-to-concept implicit conversion: for example,
`Reason reason = context.Key;` when `Reason` is a String concept, or a Guid value passed to a Uuid
identifier concept. The primitive must match the modeled type, and the operator must belong to the
exact generated Common concept symbol. Positional and named constructor arguments and typed locals
use the same admission rule. Null-literal and null-default operands converted to concepts remain
refused. Concept-to-primitive conversion still requires `.Value`; projection-source concept-to-concept
operators, record equality, inherited operators and arbitrary user-defined operators are not admitted.
Production analysis mirrors only the allowed operator, and Debug parity specs compare its full bound
identity against the real rendered compilation for classic and self-reducer profiles.

The renderer admits verified pure C# Boolean named rule bodies and code-validation iterators on commands and concepts,
using generated app-local contexts and Arc validators without a Screenplay runtime dependency.
The context supplies `Artifact`, `Value`, `Property` and `IsWholeArtifact`; command shapes exclude
server-generated properties. `Tenant`, `CausedBy` and `Occurred` reads are refused with
`STAGE-ESM-015`, not supplied from ambient state. Authored messages and severities are preserved,
and every failed rule blocks the command regardless of severity. Missing bodies fail
`STAGE-ESM-020`, envelope or compile errors `STAGE-ESM-019`, descriptor mismatches
`STAGE-ESM-021`, and forbidden pure-body symbols `STAGE-ESM-022`. Optional validated concepts
with body rules remain refused. Iterator bodies emit `IEnumerable<string>` methods wired through `.Custom(...)`,
adding an error for each yielded message in order. Only iterator validation roles admit `yield return` and
interpolation; each interpolation is emitted through `FormattableString.Invariant`, including number and
`DateTimeOffset` formatting, so messages do not depend on the host culture. Holes must use audited scalar
formatting types and pure expressions; arbitrary object formatting and all other forbidden constructs remain refused.
Boolean rules, reducers and policies still refuse interpolation and iterator syntax.

V26's Date/DateTime comparisons in command requirements, `produces when` and validation rules execute in the
semantic Host and specification executor, including through concepts. DateTime compares UTC ticks and requires
an explicit input offset or `Z` after authorization; Date compares calendar days. `then` expectations remain
ordinal.

The Cratis renderer renders these comparisons: `CompareTo` over `DateOnly` values or `DateTimeOffset` values built
from the literal's UTC ticks, in requirements, `produces when` and validation rules (absence passes). Each compared
DateTime command property carries `[property: ExplicitOffset]` (from `Cratis.Arc.Commands`), so Arc refuses input
without an offset or `Z` after authorization with the reference message. The rules render as server-side `.Must`
predicates, so no client-side date comparator is extracted. Reactions do not admit date comparisons. Three-way
conformance covers `DateComparisonsCorpus` in all four source forms.

Divergences and refusals (`STAGE-ESM-046`, naming the property):

- A compared DateTime property typed through a concept binds as a plain `DateTimeOffset`, because Arc's marker
  supports only `DateTime` and `DateTimeOffset`; it converts to the concept where an event or rule uses it. A concept
  with compliance, an optional concept-typed compared DateTime (an absent `DateTimeOffset` has no conversion back to
  an absent concept), or a property whose name is not camel case (Arc names the member in camel case), is refused.
  Pending Arc ask: a marker for concept-wrapped dates (Cratis/Arc#3350).
- For a command with an authorization policy, an unauthorized caller sending an unbindable date body gets
  `Validation`, not 403. Stage renders native named Arc policies, and Arc's resource-free evaluation covers only
  ASP.NET requirements. See [command input binding](https://github.com/Cratis/Arc/blob/main/Documentation/backend/csharp/commands/input-binding.md)
  and Cratis/Arc#3341.
- Rendered `then` expectations compare DateTime values as instants, while the reference compares spelling; the
  binder already requires expected event values to match the produced spelling.
- A comparison whose operand comes from a protected command read is not rendered for dates and stays refused.
- The legacy engines still refuse these comparisons with `STAGE-ESM-046`.

V28 admits Uuid `==` and `!=` in command `require` conditions and `produces when` guards. The semantic Host and
specification executor compare Uuids ordinally in canonical lowercase hyphenated spelling, including through concepts and
protected read paths, and reject a non-canonical input before the requirement runs. The Cratis renderer renders property
and literal comparisons: concept properties compare by `.Value` and a literal compared with a Uuid operand becomes
`Guid.Parse("…")`, so the comparison is a Guid comparison rather than a Guid-to-text one. A comparison whose
operand is a protected read path, such as `require PlantAccessGrant.memberId == requestedBy`, renders too: the renderer
resolves the read operand's type through the read scope, so it compares as a Guid like any other operand. A read path in
`produces when` keeps `STAGE-ESM-044` (see [Protected reads rendering](protected-reads-rendering.md)). The legacy
engines refuse every Uuid comparison with `STAGE-ESM-049`, naming the command and property, because they drop a
right-hand property and do not check canonical spelling. Three-way conformance over the `uuid-comparisons/v28` vector
proves that the reference, the executor and the rendered application agree, and `when_comparing_uuid_read_path_equality` proves the same for the granted and denied read-path cases.

ESM v29 (Screenplay decision 0098) adds three constructs the semantic engines handle as follows:

- `decimals <= N` on a Decimal concept or command property is checked by the executor and the Host with the reference rule (exact decimal arithmetic, trailing zeros ignored) and rendered as a generated validator, so a third decimal on a `Money` value is refused where it enters.
- `add`/`subtract <property> by <a> * <b>` in a projection is run by the executor and the Host (Play) with exact decimal arithmetic, then converted to the type of the total like any operand. The renderer lowers it, because Chronicle's projection engine adds one event property: the event record carries the product as a computed, serialized member (`public decimal UnitPriceTimesQuantity { get; init; } = (decimal)UnitPrice.Value * (decimal)Quantity.Value;`, whole numbers multiply `checked`) and the projection adds or subtracts that member. It is admitted in a `from` block when both factors are required whole or decimal properties of the event and the total holds the same kind of number (a decimal total for a decimal product, a whole total when both factors are whole); anything else (a `join` or `every` block, an optional factor, a literal factor, a total of another number type, a member name the event already has) is refused with `STAGE-ESM-050`, naming the projection and the reason. Events must be appended through the generated event types for the member to be present. Converting such a projection for Chronicle (`ProjectionConverter`) still reports `STAGE-PROJECTION-004` at the mapping instead of converting the sum of one factor, and the specification runner's per-run Chronicle projection does not run it.
- A concept mapped to another concept over the same primitive needs no special handling: both are the same primitive at run time and in generated code.

An application toolbar button declared in a layout's or module's `navigation topbar` region (decision 0098) is carried as Scene's `NavigationButton` (Cratis/Scene#215): its slice as the qualified id the structure knows, its emphasis and icon, and its `authorize` requirement as the policy expression it was written as. The region draws it after the items and opens the slice's task dialog over the current page; a module's buttons follow the items' inherit, extend and override rules. The converter no longer reports `STAGE-SCENE-NAV-001`.

ESM v30 adds two more: a command whose `produces ... for <property>` names a required scalar property other than its identifier appends to several event sources (run and rendered like any command), and `given generated <Command>.<property> = <value>` pins the generated value of a command a reaction invokes. The executor feeds each invocation the next value pinned for its command and property in authored order, and the generated reaction specification carries the pinned value as the generated fixture of the invoked command. An invocation with nothing pinned stays `Unsupported`.

V27 admits read models with several `key` parts. The semantic Host runs them end to end: `parts` projection keys
and removals, list queries with equality filters, single-instance queries by parts and the read-model browser, which
shows each instance key as an object keyed by part name. The specification executor refuses a projection into such a
read model as `Unsupported(Projection)`, naming ESM v27 composite read-model keys, because its per-run scenario seeds
and looks up instances by one event-source key. The Cratis renderer renders such a read model with a synthetic `Id`
key record (`<ReadModel>Key`) holding the parts, alongside the declared properties, and a scoped projection that sets the
parts through Chronicle's `UsingCompositeKey`, for `from` blocks and `remove with` alike. List and filtered queries
work as for any collection. Shapes the renderer cannot express are refused with `STAGE-ESM-048`, naming the reason:
reducers and flat projections, joins, children, nested objects, `every`, optional, collection, date-time or non-primitive key parts, a property named `Id`,
`from` or removal keys that are not a complete `parts` key, and protected live queries. `STAGE-ESM-047` names each
single-instance query by parts, because Chronicle cannot look up one read-model instance by a composite key until
Chronicle#4265. Generated specifications that project into such a read model are not yet admitted
(`STAGE-ESM-011`). The legacy engines refuse a read model with several `key` marks with `STAGE-ESM-048`.

For example, authored `9007199254740991`, `-9007199254740991` and `1234567890123456` render as
`9007199254740991L`, `-9007199254740991L` and `1234567890123456L`; the fraction
`0.10000000000000002` renders as `0.10000000000000002m`. Command inputs, event payloads,
validation operands, projection constants and generated specification expectations preserve those
retained ESM values.

A retained non-route number whose legacy lowering would lose digits selects language/semantic v11
and schema 11. Already-lossless route literals and route-feeding inputs alone keep their earlier
version. Reading a v1–v10 model does not change its numbers. Source still uses Double mode with whole
numbers limited to ±9007199254740991; v11 neither admits `numbers exact` nor promises arbitrary-precision
arithmetic. Generated fractions use C# `decimal`, not the full binary expansion of a Double value.

Moving an existing model from v7 to v8 widens stored event schemas from int32 to int64. Chronicle refuses
that change within the same generation; plan a new event generation before upgrading
([Stage #204](https://github.com/Cratis/Stage/issues/204)). TypeScript properties remain `number`.

## Generated C# global usings

An application render writes `GlobalUsings.cs` at the backend root with `global using` directives for
`Cratis.Arc.MongoDB`, `Cratis.Chronicle.Keys`, `MongoDB.Driver` and `System.Reactive.Subjects`. Generated
files then leave out per-file usings for those namespaces. Where a name exists in two of them, such as `Tag`,
it is qualified only as far as needed to resolve the collision. A scoped render keeps the per-file usings.

`GlobalUsings.cs` is owned by Stage, like `Program.cs`, and is regenerated on every application render, so
edits to it are overwritten. Put application-specific global usings in a separate file, for example `Usings.cs`.

## Generated C# member names

Stage appends `_` only when a command input property's generated name collides with its
command type, a record member, `Handle`, or `GetEventSourceId`. For example, command
`Text` with input `text` renders property `Text_`, JSON field `text_`, and proxy field
`text_`. Validation uses the escaped member too. Events and read-model properties retain
their names. Hand-written implementation bodies must use the emitted command members.

A query colliding with its read-model type, `EqualityContract`, `Clone`, or a read-model
property also receives `_`. Its proxy export follows the emitted method name; an Arc
`[Path]` preserves the original HTTP route using the scaffold's route settings.

A multi-field command response renders as record `<Command>Response`. When a response
field, a sibling declaration, or a namespace already uses that name, the record and its
proxy type are emitted as `<Command>Response_`; the response fields keep their names on
the wire. For example, command `Move` beside command `MoveResponse` returns
`MoveResponse_`.

`STAGE-ESM-050` reports each escape as information and does not block rendering.
Non-colliding members retain their existing output.

`STAGE-ESM-012` still refuses duplicate normalized command properties, command types
named `Handle` or `GetEventSourceId`, unsafe event/composite/response/concept members,
unsafe read-model properties, identical query overloads, and colliding query parameters
(including the injected `readModels` argument). Chronicle property-path collisions remain
`STAGE-ESM-017`. These refusals identify the declaration rather than renaming stored data.

## Generated C# observable list queries

An `observable ReadModel[]` query renders as `ISubject<IEnumerable<ReadModel>>`, returning
Chronicle's `readModels.Observe<ReadModel>()` directly. A `by` key and equality filters
become the expression passed to `Observe`. Chronicle compiles it once and evaluates it
client-side after releasing the read models; it does not narrow server reads. Results are
eventually consistent with the materialized projection or reducer, not command-side decision state.
The first emission is the current collection, including an empty collection, followed by changes.

A plain GET can return HTTP 202 until the subject emits. Use `?waitForFirstResult=true`
to wait for the first result (HTTP 200), or `Accept: text/event-stream` to receive SSE
`data:` frames as the collection changes. Generated `src/stage.ts` routes include
`waitForFirstResult=true`, so list screens do not mistake a pending result for an empty list.
The runtime frontend subscribes to those routes over SSE, so list screens update when another
client's command changes the data. The Host streams live (`observable`) queries; snapshot queries
still fetch one result and refresh after commands.

Without `observable`, a list query renders as `Task<IEnumerable<ReadModel>>` using
`GetInstances` and in-memory filtering. Snapshot lookup arguments retain their concept types.
Live-list companion specifications append fixtures to `EventStoreForTesting` and await
an emission from the generated query, executing its actual filter expression. Protected
observable collection-success specifications use Arc's `QueryScenario` with its default Chronicle
read models. Scoped projections remain refused with `STAGE-ESM-011`; routed replay requires a
readable generated `Key`, `Subject`, or `Id` on every pinned row. Protected snapshot collection
success remains refused because Arc 22.87's `GetInstances` does not read scenario-seeded rows.
Both allowed and denied protected queries run through Arc's authorization pipeline with the fixture caller.

## Generated C# query filters

ESM v14 equality filters render on snapshot and observable collection queries, with
or without a `by` argument. Parameters follow the key in declaration order; all supplied
filters and the key combine with AND. Optional filters render as `T? name = null`.
Observable expressions use `name == null ||` predicates because expression trees cannot
contain `is` patterns; snapshot predicates retain `name is null ||`.
Concept-typed filters run their concept validation rules through Arc, just like concept-typed
query keys. An omitted optional filter is not validated.

A required filter renders as a non-nullable parameter. Arc rejects missing value-type
and concept arguments with `MissingArgumentForQuery`. Arc treats plain reference
parameters as optional, so Stage explicitly guards required text filters with that same
validation failure before reading any rows. The Host instead returns the reference
`Contract` rejection. Neither path silently widens a query with a missing required filter.

An omitted optional filter does not narrow the result. The reference refuses an explicitly
supplied null; generated HTTP queries cannot distinguish explicit null from omission and
skip it. This is a known live difference. The semantic Host preserves supplied null values
and lets the reference reject them.

Snapshot collection queries are admitted. A filter that cannot preserve scalar
comparison or parameter declaration order also fails `010`, naming the query and filter.
This includes DateTime filters: the reference compares retained text, whereas CLR equality
compares instants and would merge distinct offset representations.
Generated parameter collisions fail `STAGE-ESM-012`. Filtered companion specifications
pass named filter values and assert row counts as well as expected rows. The independent
specification executor and Host specification admission support scalar equality-filtered assertions
over projected and seeded read-model state. Supplied filters combine with `by` using AND;
omitted optional filters do not narrow. Results compare by read-model identity and key without
regard to order, including row counts. Projection shapes the per-run scenario cannot preserve
still receive typed refusals. The executor supports complete scalar per-key seeds and folds later facts over them.
Generated specifications with keyed absence assertions also admit complete scalar seeds for independent
projections. A seed replaces that key's Given history; subsequent command or append facts update or remove
it. Root removals render `RemovedWith` keyed by the authored event property when one is supplied, without
removing other seeded keys. Presence at a different key can be asserted alongside absence, including without
an action. Partial or null seeds, seeded decision reads, reducers, dependent scopes and seeded query
combinations remain `STAGE-ESM-011`; the existing query-only lookup of one complete seeded record is unchanged.
Models without collection queries keep their existing generated bytes.

## Generated C# concept compliance

ESM v13 renders personal-data concepts with `[PII]` and operational secrets with `[Encrypted]` plus
`[NotAudited]`. Personal classification takes precedence when both are present. The markings travel
with the `ConceptAs` type and protect different things: `[Encrypted]` and `[PII]` protect event payloads,
while `[NotAudited]` and `[PII]` keep command inputs out of Arc's command causation chain. `[NotAudited]`
does not protect event payloads or logs. Identity uses and enum concepts refuse with `STAGE-ESM-040`
before emission.
See [Concept compliance rendering](compliance-rendering.md) for scope mapping, registration caveats
and the difference between generated applications and Play.

## Generated C# enumerations

Chronicle stores enum members as integers, with an `integer` event schema and `x-enumNames` metadata.
For numbered enums, ESM v12 carries each member's int32 identity and Stage preserves it in C#:

```csharp
public enum InvoiceStatus
{
    Draft = -1,
    Issued = 4,
    Cancelled = 90,
}
```

Negative and sparse numbers are valid; `-0` becomes `0`. Reordering numbered members does not change
their stored identities. **Never renumber a published member or reuse its number.** Specifications and
conditions still compare member names. The world rebuilder maps a stored integer through the declared
numbers, accepts declared model names, and refuses unknown numbers, fractions and undeclared names.

Unnumbered enums keep implicit declaration-order numbers: the first member is `0`, the next is `1`,
and so on. For those enums, **only append members; never reorder or remove existing members**.
Reordering changes which member a stored number identifies, and Chronicle's schema compatibility check
does not detect that remapping. Pins equal to declaration positions preserve the earlier ESM version
and generated bytes. Numbering every member with its existing identity avoids future reorder hazards.

The Host's event store is for the Host only. The Host registers its own event schemas, and they differ from the
schemas a generated application registers: enums are strings rather than integers, and identifiers, dates,
numbers and optional properties use different formats. Do not point the Host and a generated application at the
same event store. Chronicle refuses the second registration of a changed schema with `EventTypeSchemaChanged`.

## Match patterns

`matches` uses Screenplay's portable match dialect: case-sensitive UTF-16 matching without culture-dependent
character classes. `\d` and `\w` are ASCII; `\s` includes Unicode spaces and the byte-order mark (`U+FEFF`),
but not next-line (`U+0085`). Dot excludes LF, CR, line separator (`U+2028`) and paragraph separator (`U+2029`).
An unescaped `$` outside a character class means end of input, not the position before a final newline:
`^[A-Z]{3}$` accepts `ABC` but rejects `ABC\n`. Escaped dollars and dollars inside character classes remain literal.
Unanchored patterns search the input rather than requiring a whole-input match.

The semantic executor and Host delegate match evaluation to Screenplay. Generated C# validators expand portable
character sets and use strict anchors with .NET's non-backtracking engine; they do not turn timeouts into
validation failures. If .NET cannot compile an otherwise portable pattern (for example, an empty group with a
counted repeat larger than .NET's integer bound), rendering refuses it with `STAGE-ESM-005`, naming the construct.
The renderer also retains .NET's non-backtracking automaton-size limit rather than changing process-wide settings.

## Chrome

The topbar, sidebar, breadcrumb, footer and settings panel are the blueprint's own. The Stage fills them: the breadcrumb shows
where the current screen is, the topbar carries the blueprint's theme switcher and, when the model declares more than one locale,
a locale picker, and the menu is one of two things.

### Navigation regions

A model that declares `navigation topbar` and `navigation sidebar` (in the layout, and per module with `inherits`, `extends` or
`overrides`) gets the shell's own regions: the blueprint's topbar and sidebar draw the **effective navigation** of the module
the user is in (Scene's `effectiveNavigation`; the Stage keeps no navigation tree of its own). Going to another module swaps the
sidebar, the item of the page is marked (`aria-current`) in both regions, an item renders into the slot it names, and an item
that targets a module opens the module's first feature. The Stage tells Scene which screen each feature opened (`screenFor`: the
screen declared in the feature) and which module a screen belongs to (`moduleOf`: the first of its `path`).

Where the user is is the address: `#/OrderList` restores the module, the feature and the slots after a reload or from a shared link.
`App` takes an `isVisible(item)` predicate that hides an item - hidden, never disabled, so nothing about it leaks - at every level of
a region; a host that knows what the user may see plugs it in.

The scene a running Stage serves carries the model's `structure` (modules, features and slices) and `commandSchemas` (the JSON
schema of every modeled command) only when a toolbar button names a slice; an application without one is byte-identical to what it
was before slice buttons existed.

### Toolbar slice buttons

A toolbar button that names a State Change slice opens the slice as a task dialog over the page - Scene's `SliceDialogView`, the
same one the Studio preview shows. Its fields are what the command's schema declares, read the way the native command form reads
it (a property the form cannot present, such as a collection, is left out rather than shown as a field that cannot be filled).
Submitting posts the command to the route the Stage registered for it, through the Stage's slice invoker, exactly as an action or a
form posts it: the application's own validation results come back to the dialog against the fields they name, a rejected command
keeps the dialog open, and a successful one closes it and refreshes what the screen shows.

### Root template, options and appearance

A `ui profile` can name the application's shell, configure it and set its look:

```screenplay
ui profile Web
  root template blueprint Cratis.Blueprint.Default AppShell version "^4.5"
    option navigationMode slim
    option showFooter false
  appearance
    color scheme dark
    accent "#4f46e5"
  new screens use template MasterDetail
```

* The shell is chosen by Scene's `resolveEffectiveShell` (in .NET `ShellResolver`, through `RootShell.Resolve`) - the function the Studio preview
  calls, so the Stage and the preview start in the same shell: `root`, then the legacy `layout`, then the default blueprint's layout,
  then the bare shell. Nothing in the Stage re-implements the order.
* `option navigationMode` (`static`, `overlay`, `slim`, `slim-plus`, `compact`, `horizontal`, `reveal`, `drawer`), `option menuTheme` and `option showFooter` are applied to the shell; an option or
  value the shell does not understand is reported, never silently dropped.
* `color scheme` (`light`, `dark`, `system`) selects the dark or light theme (a `system` scheme follows the operating system), and `accent`
  becomes the theme's primary colour.
* A root template the application cannot start with - unknown, blueprint not installed, version pin not satisfied, no `content` outlet - is
  a **build-time finding** when planning (`cratis render`, the render plan) and, in a running Stage, an explicit start-up message that names
  the cause. The Stage never falls back to a different shell, and never shows a blank page.
* The runtime scene carries `root`, `appearance` and `defaults` and, as always, no sample data (see below).

### Navigation items that render into a shell slot

A navigation item authored with `slot <name>` renders its target into that outlet. When the outlet is one the shell declares - `side` in
`layout AppShell ... side`, not a `core:outlet` some screen draws - the Stage hosts it: the page stays in the primary outlet, the target
is drawn in a closable pane beside it, both items are marked active (`aria-current`), `Close` empties the pane, the pane stays when another
page is opened and is restored from the address after a reload or a deep link. A shell without that slot opens the target in the primary
outlet, as an item with no `slot` does. The Studio preview and a rendered application behave the same: Scene's `ApplicationHost` composes them all, and `navigationParity.spec.tsx` clicks the same items in the Stage and in the preview host and requires the same pages, panes and marked items.

### No sample data in production

Sample data exists to preview with at design time. The runtime document and the scene a running Stage serves carry **no sample
data**: no packs, no blueprint packs, and no selection in a profile (`SceneApplication.ForRuntime`). The browser scenarios assert
it on the live Stage and on the generated application.

### One composition for the Stage and the preview

The Stage composes and hosts a screen with the same functions the Studio preview does - `composeApplicationScreen` and
`ApplicationHost` from `@cratis/scene.blueprint.default`. The Stage decides only what is its own: the chrome around the shell, its
command form, its status line, its data sources, and the data and command scope it provides around the screen and the dialogs. The
template wrapper and the flow arrangement view the Stage used to keep are Scene's.

### Authored navigation hierarchy

A model that declares no navigation regions keeps what it always had. When a feature declares `contribute to Navigation` entries, the menu is the application hierarchy the model wrote
instead of a flat list of screen names: a group for the module and for each feature, nested as deep as the model
nests features, with each entry under the feature that contributed it and ordered by its `order`. An entry links to
the screen it opens; the current screen is marked, and the breadcrumb shows the groups leading to it. A feature's
entry reaches the runtime on every screen of that feature and is shown once. A model with no navigation entries keeps
the flat list of screens.

A navigation entry carries everything it was authored with. Its `id`, `icon`, `presentation` and `group` reach the
runtime. The `id` is the entry's stable identity, so there is one entry per `id`. Entries that share a `group` are listed
under a section heading of that name within their feature, and the breadcrumb leaves the section out. A `destination outlet|dialog|external` composes with its `navigate to` into one Scene destination: the
entry opens in the named outlet, opens the named dialog, or leaves the application. The `presentation` hint decides
what the menu shows - `icon`, `text` or `both` (the default) - and an icon-only entry keeps its label as its accessible
name. An entry that opens somewhere a plain link does not reach is executed by the Scene navigation host, the same as a
toolbar item.

A module's own `contribute to` reaches every screen beneath the module, recorded against the module alone, and a
`contribute to` written inside a screen records the module and features the screen sits in. Every screen carries
that place as `path`: the modules and features, outermost first, it was declared in. A screen no entry lists is
shown under its own module and feature - beside the entries that do list screens there - instead of a single
"Other screens" group, and the breadcrumb leads to it through that place. Only a screen declared outside any module
has no place to be under and goes under "Other screens".

### Navigation host, outlets, dialogs and toolbars

The Stage does not keep a navigation state of its own. Where the user is - the screen in the primary region, the screens
in nested outlets, the dialog on top - and the address (`#/Screen?parameter=value`) that reaches it belong to the Scene
navigation host (`useSceneHistoryNavigation` over `createHashSceneHistory`), the same way a generated or embedded host
runs them. Destinations resolve through Scene: a `core:outlet` renders the screen placed in it inside the Stage's data
scope, a dialog destination opens the authored dialog template and closing it goes back, and an address that stands for an
outlet destination restores the whole composition (the list with its details beside it), not just the details.

A `toolbar` renders as `core:toolbar` with a `core:toolbarItem` for each item. An `action` item announces its command with
its parameters read from the screen; a `navigate` or `dialog` item carries the destination it opens (`navigate` to the screen,
or the dialog template) and executes it through the host. An item's `presentation` is honoured: `mode` is `icon`, `text` or
`both`, and every other hint (`placement`, `density`, ...) is exposed to themes as a `data-scene-presentation-<name>` attribute.

Themes are the blueprint's (`Scene Default Light` and `Scene Default Dark`). The active theme's colour scheme is
mirrored onto the document root so PrimeReact overlays, which are portalled to `body`, follow it.

## Profile inputs the target cannot honor

A `ui profile` that activates a package the target does not render, selects a layout, theme or root template that does not
exist, or names packages that cannot be used together would publish an application with a different shell or unstyled
components. `CratisRendering.ResolveProfileInputs(scene, approvedPackages)` resolves every profile with the Scene engine's own
`RenderPlanner` and returns one `CratisProfileInputFinding` for each input the target cannot honor, so a renderer consumer
refuses before it writes or generates anything. The CLI and Studio call it, so a changed profile input is refused the same way in both.

| Code | A profile input that |
| --- | --- |
| `STAGE-PROFILE-001` | activates a package the target does not render and no approved package set supplies |
| `STAGE-PROFILE-002` | selects a layout neither the application nor an active package declares |
| `STAGE-PROFILE-003` | names packages that cannot be used together (incompatible theme, missing or conflicting dependency, dependency cycle) |
| `STAGE-PROFILE-004` | selects a theme the application does not declare |
| `STAGE-PROFILE-005` | names a root template that cannot be the shell (missing, blueprint not installed, version pin unsatisfied, no content outlet) |
| `STAGE-PROFILE-006` | references a blueprint, version or template the approved blueprint does not provide |

## Command execution

Forms, guarded actions, interactions and slice task dialogs execute registered Arc `Command` proxies. In rendered
applications, `src/bindings.ts` registers the generated proxies. Commands honor the enclosing `ArcContext`'s
microservice, API base path, origin and `httpHeadersCallback`, including identity headers. Only properties declared
by the proxy are submitted; responses retain the proxy's response type. Arc checks required properties in the
browser before making the call, enforcing the same required-property rule as the server. Validation failures, authorization denials
and exceptions remain failed outcomes and do not refresh the screen or expose a success response.

The live Stage registers equivalent route-backed proxies before its first render. Both live engines expose
`commandSchemas` alongside `commands` and `queries` at `/stage/routes`: a map from command name to its JSON schema
string, including commands used only by interactions or guarded alternatives. In the semantic engine, generated
properties are server-owned and are not proxy inputs. `commandResponseKinds` describes the response from the modeled
return metadata so proxies preserve strings, numbers, booleans and records. Commands without a declared return retain
the host's object echo. Commands register under their full type identity and route, with simple-name aliases only for unambiguous names.
Missing schema metadata produces a console warning and leaves that command unregistered; other commands and the
live UI remain available. If `/stage/routes` cannot be loaded, Stage warns and renders the scene without registered
commands. This metadata does not add schemas to the scene
document of an application without toolbar slice references. Query reads still use the Stage query transport.

## When the model declares no screens

A model with no `screen` still renders. The Stage synthesizes one screen per slice that carries a command or a
read model and places it in the same shell. That is a fallback for a model that has not designed its screens yet;
as soon as the model declares its own screens they are what is drawn.

## Multi-service repository layout

`CratisPlanOptions.ServiceLayout` (and `CratisRepositoryPlanOptions` for the repository root) renders one
application as a service of a repository that holds several. It defaults to `false`, and then every plan is
byte-identical to the plan without the option: no path changes, no input joins the profile, and the
digest is unchanged.

With `ServiceLayout = true` the profile carries the hashed input `cratis-render:service-layout`, version
`1`, with exact UTF-8 bytes `service`. Every artifact moves, byte for byte, into `Source/{ProjectName}/`,
including scaffold, source plans, scoped plans, implementation-gap stubs and required removals. The files
the repository owns are left out: `.editorconfig`, `Directory.Build.props`, `Directory.Build.targets`,
`Directory.Packages.props`, `{ProjectName}.slnx` and `docker-compose.yml`. Files with those names in a
sub-folder are application files and stay. Pass the same options, together with the same `Profile`, to
every planning call. Planning never moves an existing repository; the caller decides to switch layouts.

`CratisRendering.PlanRepository` plans those repository files: the shared build files, a solution that
lists every service and `Source/Composition`, `docker-compose.yml`, and an Aspire application host that
starts Chronicle and every service. `Composition` is reserved for the host.

| Diagnostic | Severity | Meaning |
| --- | --- | --- |
| `STAGE-PLAN-028` | Error | `ServiceLayout` is combined with `RepositoryRoot`, which would give one application the repository-root files it must leave to the repository plan. |
| `STAGE-PLAN-029` | Error | `PlanRepository` options are invalid: no services, a name that is not a dot-separated C# identifier, `Composition`, a service whose Aspire resource name is `chronicle` (any casing), which the application host reserves for Chronicle, a duplicate service ignoring case, or two services that map to the same Aspire resource name. |

Both codes are raised only for invalid option combinations, never for model constructs.
