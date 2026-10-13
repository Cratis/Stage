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
| `ui profile` | Decides which packages a bare component name resolves against. `core`, `PrimeReact` and the blueprint are always admitted. |
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

## Generated C# query arguments

Optional snapshot lookups by identifier and filtered observable collections accept scalar concepts
with validation rules. Stage renders these query arguments as their underlying primitive and
constructs the concept inside the method. An out-of-rule key returns `null` or an empty collection,
not an Arc validation failure, matching the Screenplay reference semantics. Command properties
remain concept-typed and still validate. Concepts without rules retain their concept-typed query
parameters; TypeScript proxy argument types are unchanged.

A composite or collection query argument containing a validated concept fails admission with
`STAGE-ESM-010`, naming the query and concept.

## Generated C# whole numbers

The semantic Cratis renderer maps whole-number primitives and concepts to `long` for ESM v8 and later,
and to `int` for ESM v1–v7. Older models with authored whole-number literals outside the `int` range
fail admission with `STAGE-ESM-031` rather than generating uncompilable C#.

Stage uses Screenplay 4.146.0 and admits exact ESM v1–v14 language/semantic pairs, including v11's corrected Double-mode literal lowering.
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

## Generated C# query filters

ESM v14 equality filters render on observable collection queries (`Many` + `Live`), with
or without a `by` argument. Parameters follow the key in declaration order; all supplied
filters and the key combine with AND. Optional filters render as `T? name = null` and
`name is null ||` predicates. Validated concepts accept primitive arguments and are
constructed inside the query, without applying command validation rules.

A required filter renders as a non-nullable parameter. Arc rejects missing value-type
and concept arguments with `MissingArgumentForQuery`. Arc treats plain reference
parameters as optional, so Stage explicitly guards required text filters with that same
validation failure before reading any rows. The Host instead returns the reference
`Contract` rejection. Neither path silently widens a query with a missing required filter.

An omitted optional filter does not narrow the result. The reference refuses an explicitly
supplied null; generated HTTP queries cannot distinguish explicit null from omission and
skip it. This is a known live difference. The semantic Host preserves supplied null values
and lets the reference reject them.

Snapshot collection queries still fail `STAGE-ESM-010`; admitting them belongs to
[Stage #58](https://github.com/Cratis/Stage/issues/58). A filter that cannot preserve scalar
comparison or parameter declaration order also fails `010`, naming the query and filter.
This includes DateTime filters: the reference compares retained text, whereas CLR equality
compares instants and would merge distinct offset representations.
Generated parameter collisions fail `STAGE-ESM-012`. Filtered companion specifications
pass named filter values and assert row counts as well as expected rows. The independent
specification executor refuses filtered query assertions with `STAGE-ESM-010` rather than
ignoring their filters; seeded read-model specifications retain the `GivenReadModel` gap.
Models without filters keep their existing generated bytes.

## Generated C# concept compliance

ESM v13 renders personal-data concepts with `[PII]` and operational secrets with `[Encrypted]` plus
`[NotAudited]`. Personal classification takes precedence when both are present. Protection travels
with the `ConceptAs` type; identity uses and enum concepts refuse with `STAGE-ESM-040` before emission.
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

`matches` uses ECMAScript regular-expression semantics with culture-invariant matching and a one-second timeout.
An unescaped `$` outside a character class means end of input, not the position before a final newline:
`^[A-Z]{3}$` accepts `ABC` but rejects `ABC\n`. Escaped dollars and dollars inside character classes remain literal.
The semantic executor, Host and generated C# validators use the same rule.

## Chrome

The topbar, sidebar, menu, breadcrumb, footer and settings panel are the blueprint's own. The Stage fills them:
the menu lists the model's screens, the breadcrumb shows the current one, and the topbar carries the blueprint's
theme switcher and, when the model declares more than one locale, a locale picker.

### Authored navigation hierarchy

When a feature declares `contribute to Navigation` entries, the menu is the application hierarchy the model wrote
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
