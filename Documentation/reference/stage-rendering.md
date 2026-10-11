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

## Generated C# whole numbers

The semantic Cratis renderer maps whole-number primitives and concepts to `long` for ESM v8 and later,
and to `int` for ESM v1–v7. Older models with authored whole-number literals outside the `int` range
fail admission with `STAGE-ESM-031` rather than generating uncompilable C#.

Moving an existing model from v7 to v8 widens stored event schemas from int32 to int64. Chronicle refuses
that change within the same generation; plan a new event generation before upgrading
([Stage #204](https://github.com/Cratis/Stage/issues/204)). TypeScript properties remain `number`.

## Generated C# enumerations

Chronicle stores enum members as integers, with an `integer` event schema and `x-enumNames` metadata.
Stage currently generates implicit C# member numbers in declaration order: the first member is `0`,
the next is `1`, and so on. The world rebuilder accepts those numbers and declared model member names,
normalizing both to the model name. It refuses out-of-range or fractional numbers and undeclared names.

Until [Screenplay #643](https://github.com/Cratis/Screenplay/issues/643) carries stable member numbers in
the model, **only append members; never reorder or remove existing members**. Reordering changes which
member a stored number identifies. Chronicle's schema compatibility check does not detect that remapping.

The chosen direction is to carry explicit member numbers in the model and preserve them when rendering.
Names on the wire do not solve this: Chronicle still registers an integer schema and converts names to
numbers on append, and Arc uses numeric enums. Hashing names was rejected because renaming a member
would change its number and existing applications would be renumbered.

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

Themes are the blueprint's (`Scene Default Light` and `Scene Default Dark`). The active theme's colour scheme is
mirrored onto the document root so PrimeReact overlays, which are portalled to `body`, follow it.

## When the model declares no screens

A model with no `screen` still renders. The Stage synthesizes one screen per slice that carries a command or a
read model and places it in the same shell. That is a fallback for a model that has not designed its screens yet;
as soon as the model declares its own screens they are what is drawn.
