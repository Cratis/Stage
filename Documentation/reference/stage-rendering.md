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
| `ui profile` | Decides which packages a bare component name resolves against. `core`, `PrimeReact` and the blueprint are always admitted. Its `root template`, shell options and `appearance` decide the shell the application starts in (see [Root template, options and appearance](#root-template-options-and-appearance)). |
| Command and query | Components wired to the routes the running application exposes. |

A bare component name in a template (for example `title` or `inputText`) is resolved against the profile's
packages before it is rendered, so a template written against the blueprint's vocabulary draws real components
rather than placeholders. A name no package declares renders a placeholder that names what is missing.

## Guarded actions and interactions

A simple `action … execute <Command>` translates to `core:action`. A guarded action with ordered
`when … execute` alternatives and an `otherwise` also translates to `core:action`, carrying its alternatives,
their guards and the fallback. The Stage frontend runtime, which a live Stage and a generated React application
both render with, evaluates the guard against the item the screen's nearest `data` declaration reads:

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
event-source identity checks retain the classic semantics. The scaffold pins Chronicle
19.74.2 and Arc 22.109.0 for this generated surface.

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
Migration of existing applications is not available yet.

## Generated C# query arguments

Query arguments retain their concept types so Arc validates the `by` key and supplied filter
values before invoking the generated method. An out-of-rule value returns HTTP 400 with validation
results; an in-rule key that matches nothing still returns `null` or an empty collection.
Validation also reaches concept members inside composite arguments and collection elements.
Authorization runs before validation. TypeScript proxy argument types are unchanged.
Nullable keys and optional concept filters with `not empty` rules receive `STAGE-ESM-010`:
Arc skips null concept values, while the reference validates nullable concept keys and rejects
explicitly null filters. An omitted optional filter remains distinct from a supplied null.

ESM v18 adds query-only `then error` specifications. For empty fixtures, Stage generates an Arc
`QueryScenario` call that asserts validation failure and the expected message, when supplied.
These empty-world specifications also support `then no result`, but do not relax application
admission: every read model still needs a projection or reducer (`STAGE-ESM-007`). The
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

## Generated C# whole numbers

The semantic Cratis renderer maps whole-number primitives and concepts to `long` for ESM v8 and later,
and to `int` for ESM v1–v7. Older models with authored whole-number literals outside the `int` range
fail admission with `STAGE-ESM-031` rather than generating uncompilable C#.

Stage uses Screenplay 4.204.0 and admits exact ESM v1–v27 language/semantic pairs, including v11's corrected Double-mode literal lowering, v15's generation strategies with atomic freshness scopes, and v20's rules on generated concepts. V21's `generate pattern` and `generate random` render Fundamentals factories and execute with reference-compatible fixture shapes; dated patterns take their UTC date from the generation context, and random values preserve the full inclusive Int64 range. The specification executor runs pinned String `generate rule` values without invoking their bodies and still checks concept rules; missing pins remain `Unsupported(IdentityAllocation)`. The Host refuses commands that generate rule values with `STAGE-ESM-028`, naming the command, property and concept; unused rule concepts do not block the Host or executor. The semantic renderer emits verified C# rule bodies through `IGeneratableInContext<T>.New(GenerationContext context)`, compile-checked against embedded BCL and pinned Fundamentals metadata references without the pure-body walk. Fundamentals is an embedded build-only reference, not a renderer runtime or package dependency. Missing attachments fail `STAGE-ESM-020`, body compile errors and warnings fail `STAGE-ESM-019` at the attachment, and shadowing fails `STAGE-ESM-022`. Any concept rule requires a pin for rule generation; concepts with no validation rules may generate unpinned in rendered specifications. The legacy syntax engine still refuses `generate rule` with `STAGE-ESM-028`. `validate checksum luhn`, `mod11` and `mod97` render and execute. The legacy syntax renderer still refuses checksum and unknown validation rule kinds with `STAGE-ESM-042`. Generated values with any unproven rule require pins in rendered specifications; fully proven rules permit unpinned generation. Failing pins retain `STAGE-ESM-011`, and unpinned specifications with unproven rules retain `STAGE-ESM-038`. The generated backend pins Arc 22.109.0, which rejects generated values failing concept rules of any severity. Runtime generation defects produce exceptions rather than input validation failures. Applications using only implicit UUID generation remain unguarded even at later schema versions. See [Generated values](generated-values-rendering.md) for factory shapes, fixtures and timing limits.
Mixed version pairs and versions after v27 are refused with `STAGE-ESM-016`. The semantic Host and specification executor run protected command reads, read-backed requirements and produced-event mappings. The Cratis renderer renders `command reads protected`, command `reads`, read-backed requirements and read-path mappings as Arc read protection; see [Protected reads rendering](protected-reads-rendering.md). It still refuses the shapes it cannot preserve with `STAGE-ESM-044`, each naming its reason: a read path in a `produces when` condition or destination, a read-path mapping to an optional, collection, composite or differently typed event property, reads combined with generated-value freshness scopes, and reads on commands that produce constrained events. Non-Error requirements on protected commands, including reaction-invoked commands, remain refused before execution with `STAGE-ESM-045` until Screenplay defines their severity semantics. See the [semantic Host](../docker/index.md#opt-in-to-semantic-execution) for its interim outside-append fault boundary. The legacy renderer, Host and structural specification runner also refuse the preamble with `STAGE-ESM-044`; without it, reads still fail binding with `PLAY0271` and the repair diagnostic `PLAY0786`. ESM v24's `contains`, `not contains`, `starts with` and `ends with` rules execute on the Host and specification executor and render as ordinal UTF-16 string comparisons, without regex or a Screenplay dependency in generated apps. Absence passes; an empty operand satisfies contains/starts-with/ends-with and fails not-contains for every present value. In admitted v22+ models, all three engines (renderer, Host and specification executor) refuse described command handlers with `STAGE-IMPL-002`, naming the command and RequirementId, and `reaction batches atomic` with `STAGE-ESM-043`. No stubs are emitted yet, even in Stub mode.

V26's Date/DateTime comparisons in command requirements, `produces when` and validation rules execute in the
semantic Host and specification executor, including through concepts. DateTime compares UTC ticks and requires
an explicit input offset or `Z` after authorization; Date compares calendar days. `then` expectations remain
ordinal. The renderer and legacy engines refuse these comparisons with `STAGE-ESM-046`, naming the property,
for application and slice-scoped renders. No date validators or artifacts are emitted, so no client-side date
comparator is extracted. Generated offset binding and date three-way conformance await renderer support.

V27 admits read models with several `key` parts. The semantic Host runs them end to end: `parts` projection keys
and removals, list queries with equality filters, single-instance queries by parts and the read-model browser, which
shows each instance key as an object keyed by part name. The specification executor refuses a projection into such a
read model as `Unsupported(Projection)`, naming ESM v27 composite read-model keys, because its per-run scenario seeds
and looks up instances by one event-source key. The Cratis renderer refuses before planning any artifact and never
throws: `STAGE-ESM-048` names each read model with two or more key parts and each projection keyed by `parts`, and
`STAGE-ESM-047` names each single-instance query by parts, because Chronicle cannot look up one read-model instance
by a composite key until Chronicle#4265. List queries over such read models need no lookup, so they are not refused
separately. The legacy engines refuse a read model with several `key` marks with `STAGE-ESM-048`. Rendering composite
read-model keys follows in a later release.

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
They still fetch one result and refresh after commands; browser SSE push and Host streaming
are not implemented. The Host continues to serve one-shot arrays, even for `observable` queries.

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
specification executor refuses filtered query assertions with `STAGE-ESM-010` rather than
ignoring their filters; seeded read-model specifications retain the `GivenReadModel` gap.
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
runtime, and a `destination outlet|dialog|external` composes with its `navigate to` into one Scene destination: the
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

## When the model declares no screens

A model with no `screen` still renders. The Stage synthesizes one screen per slice that carries a command or a
read model and places it in the same shell. That is a fallback for a model that has not designed its screens yet;
as soon as the model declares its own screens they are what is drawn.
