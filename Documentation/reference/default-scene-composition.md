---
title: Cratis Stage default Scene composition
description: Cratis Stage generates command forms and eligible keyed lookups when no Scene is authored, selecting supported inputs and binding query proxies.
---

Cratis Stage generates a default Scene, the application's declarative UI document, when a render profile
has no authored Scene. It emits `scene.json` with command forms and eligible editable keyed lookups, plus
`src/bindings.ts` to bind them to the generated proxies. Use this default for model-derived forms and lookups;
a profile that carries an authored Scene uses it unchanged. The planner does not mutate the profile or
generate per-screen TypeScript components.

Authored profiles can carry [slice-owned screen partitions](render-plans.md#slice-owned-screen-fragments)
from the same compilation. Stage assembles the canonical app Scene from those records and emits each declaring
slice's `Screens.scene.json`, including in scoped plans. Shared Scene aggregates are application-scope only.
The default screen belongs to the application and is not copied into each slice as that slice's UI.

## Navigation, toolbar buttons and template references

`ScreenplaySceneVisitor` carries authored navigation, toolbar buttons and template references into the Scene
model. Scene validates these constructs; Stage reports the findings described below.

Each `outlet <name> of navigation` in the first declared `layout` becomes one of the application's navigation
outlets, and the topbar's `logo` its `ApplicationLogo`. The layout's actions outlet that holds a navigation outlet's
buttons (`topbarActions`) becomes the outlet Scene draws beside it (`topbarButtons`). A module's
`outlet <name> inherits|extends|overrides` of a navigation outlet becomes an `OutletRefinement` of its
`ModuleNavigation`, with the items it adds and the ids it `hide`s, and its `contribute to` with items an
`OutletContribution`. A module that changes no navigation outlet has no entry. Scene computes the items through
`EffectiveNavigation`; Stage does not compute them.

Each navigation item keeps its id, title (a `$strings.<key>` token as written), icon, `slot` and nested items. Its
`target` becomes a `DestinationReference` naming the module and dotted feature path. An application with no
navigation outlet serializes without a `navigation` member, leaving its plan digest unchanged.

A slot keeps its role (`page`, `pane` or `panel`), `routed` and the parameter it is `routed by`, `primary` and
`optional`; an unwritten word stays unstated. Every other outlet a layout, screen template or dialog template
declares becomes a Scene `Outlet` with its kind (actions, filters, tabs, stats), the slot it is `in`, what it acts
on (`for item|for screen`) and its `default` items. A module's, feature's or slice screen's refinements and
contributions of those outlets become the outlet levels Scene's `OutletChains.Of` and `EffectiveOutlets.Resolve`
resolve through (`SceneApplication.OutletLevels`); Stage does not resolve them itself. A screen's `fits` keeps its
order.

A button, `button <id> title "..." slice <Slice> emphasis <e> icon <i>`, becomes an `OutletButton` carrying its
hook: the slice as the qualified id (`Module.Feature.Slice`) the structure knows, so a suffix such as
`Orders.PlaceOrder` resolves the same way in the engine as in the compiler; the built-in it `runs` (`close`,
`submit` or `refresh`); where it `opens` (a bare `runs` is `none`, run directly); `before` or `after`; `enabled
when`, `confirm` and `authorize`. A screen's toolbar becomes the `core:outletItems` element that places an outlet of
actions, named as the toolbar (`toolbar` when it names none). A dialog template's `outlet actions of actions` is the
outlet Scene draws the dialog's buttons from.

`show <Screen> by <param> from <element>.selection` and a form field's `choose from` picker are not translated yet
and are refused with `STAGE-SCENE-UI-001` rather than dropped.

`template <Name>` and `template blueprint <Blueprint> <Name> [version "<range>"]` become `SceneApplication.TemplateReferences`, each a Scene `TemplateReference` (source, id, blueprint, version range) with the scope it was assigned at. The template's category and the slice kinds of a `scopes slice` template travel in its `TemplateMetadata`. An application with no assignment serializes without a `templateReferences` member.

A top-level `sample data <Name>` pack becomes a Scene `SampleDataPack` owned by `user`. Its id is the lower-case,
hyphenated form of the name (`NordicDemo` becomes `nordic-demo`), its title defaults to the name and its locale
to Scene's default. Row fields keep their authored literals and order, whether text, whole number, decimal,
Boolean or null. Sample data is for design and preview time only. It is never part of a production build and
never reaches the semantic model.

The `sample data` block of a `ui profile` becomes its `SampleDataSelection`: one active pack, shape mappings and
read model bindings, with every pack reference using the same derived id. `active blueprint <id>` keeps the
blueprint's id as written; the build resolves it against the packages the profile activates. The Screenplay
compiler checks whether the rows fit the read model. Scene checks whether the pack and selection are well
formed and reports a problem as a `SampleDataInvalid` finding.

To validate the assembled UI, the planner builds Scene's `ApplicationStructure` - modules, features and slices, each slice with its kind (a Translate slice is an automation: it has no user input) - from the compiled model and runs Scene's structure, navigation and toolbar validation over it. What they find is reported as `StructureInvalid`, `NavigationInvalid` and `ToolbarButtonInvalid` [findings](render-plans.md#findings), never thrown. The structure is a reference input; it is never written into the Scene document.

## Lookup eligibility

Stage checks whether it can render the backend model before composing the screen. After this semantic
admission, a default lookup requires all of the following:

- A modeled keyed query that returns a snapshot of zero or one result (`ZeroOrOne`), not a collection.
- A unique exact semantic query name across the application.
- A scalar text or UUID argument, either primitive or a non-enumerated concept over that primitive.
  These generate native Arc `String` or `Guid` parameter descriptors.
- At least one required, non-identifier, non-key **own** read-model property whose native value is a string,
  number or boolean. Text, whole number, decimal and boolean primitives and non-enumerated concepts qualify.

Eligible result properties are ordered by stable semantic property identity, ordinally; the first is selected.
The name is the actual generated member name, including Arc's leading-acronym convention, not a property path.
The canonical `ProjectById` lookup displays `name`, never its Guid-valued `projectId`.

Unsupported lookups are omitted, not replaced by inert or permanently failing components. Numeric, boolean,
date, composite and enum keys do not get an input form. Guid/date/object/collection/nullable result properties
are not chosen. A model with only a key or no supported result retains its command forms but has no lookup.
No query is invented for a model without one; if no commands or eligible lookups remain, no default Scene is
composed. Malformed, optional or collection identifiers are rejected by the semantic model before composition.

## Emitted component properties

For the `ProjectById` lookup, the emitted `Cratis.Components:queryInputForm` accepts a project UUID and displays
the result's `name` field:

```json
{
  "query": "ProjectById",
  "inputs": [{
    "parameter": "projectId",
    "type": "string",
    "label": "project id (GUID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)",
    "required": true,
    "pattern": "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}(?![\\s\\S])"
  }],
  "resultField": "name",
  "label": "Find project summary",
  "submitLabel": "Search"
}
```

The argument name comes from the same naming convention as the generated C# query parameter. The native
proxy generator emits `new ParameterDescriptor('projectId', Guid, false)` for the canonical model and
`String` for the text-key variant. Both use `type: "string"`; Scene passes entered strings unchanged.

Guid inputs explicitly require the dashed 8-4-4-4-12 hexadecimal format before HTTP, accepting either case.
No version, variant, nonzero-ID or backend authorization rule is added. Text inputs have **no pattern**,
trimming, coercion, defaults or generated IDs. Required inputs reject empty/whitespace-only drafts; other
text is exact, including surrounding whitespace. Submission, not editing, commits query arguments.

## Binding identity

Stage imports each query's actual proxy export from its declaring slice folder, with a distinct local alias,
and emits one `registerQueryIdentity(name, sourceIdentity, proxy)` call per source. `sourceIdentity` is the
query's stable semantic ID (`sem1:…`), not its entered key. No source is collapsed by name and no duplicate
legacy `registerQueries` call is emitted. Query element IDs use `query:` followed by that semantic ID, so
query and command elements cannot collide even when their display names match.

Scene's identity registry resolves a unique identity for existing authored tables and `singleResult` elements.
The legacy host registration fallback remains Scene-owned. Different sources with the same query name remain
ambiguous: both are registered, but neither gets a default lookup form. The current textual binder for v1 of the executable semantic model (ESM) also
rejects duplicate query names; registration still preserves distinct identities in supplied semantic models.
Command registration and command form behavior are unchanged. Bindings and Scene JSON contain no HTTP route.

## Package and verification boundary

Stage's live frontend is an npm workspace root at `Source/Frontend`. The host consumes the runtime workspace
at `Source/Frontend/Runtime` as `@cratis/stage.runtime`; its React, Arc, Scene and Components dependencies are
peers pinned to the host's versions so contexts and registries are shared. `npm test --prefix Source/Frontend`
runs both host and runtime specifications, and `npm run build --prefix Source/Frontend` builds the package
before the host, keeping the host output at `Source/Frontend/dist` for the Stage image.
The runtime package is published at the Stage release version before NuGet publication. A generated application
depends on `@cratis/stage.runtime` pinned to the exact Stage release that rendered it; `main.tsx` imports `App` and
the Stage source from the package and `vite.config.ts` imports the plugin from `@cratis/stage.runtime/vite`. The
render no longer emits `.frontend/stage/*` or `.frontend/styledPrimeReact.ts`. Arc, Scene, Components, PrimeReact and
React stay application dependencies, pinned to the versions the runtime declares as peers.

To migrate an application rendered before this change, render it again. The CLI removes the managed
`.frontend/stage/*` copies that are unchanged from what Stage emitted. A copy you edited is refused, and the render
stops until you delete or restore each edited copy; `--force` does not cover it. A user-owned
`.frontend/vite-env.d.ts` also stops the render.

To build against an unreleased Stage, install a packed runtime in your own working copy.
The render itself never writes a local path.

The emitted frontend takes exact Cratis package pins from Stage's `Source/Frontend/package.json`, including
Scene, Components, Fundamentals and Arc React's form custom-validation gate. The generated backend takes its
NuGet pins from Stage's `Directory.Packages.props` at build time. Frontend and backend Arc use the same release.
The generated backend references Cratis.Chronicle explicitly to match its development kernel image, instead of
relying on the client the Cratis metapackage brings transitively. Stage's package-update workflow synchronizes
the npm pins, emitted frontend package set, Host kernel image and framework-name catalog.
Published Components **4.14.0** and Scene **4.2.0** provide the command-form Guid/submit UI contract;
native build and specification checks do not prove browser functionality.

Stage tests parse the emitted JSON and build the generated Guid/text backends with the native Arc proxy
generator. Changes to this profile also require installing the exact published frontend packages, typechecking
and bundling the generated application, and verifying the native consumer contract. C# rendering tests alone
do not establish those integration gates or browser acceptance.

When you need UI or behavior beyond the generated default, continue with
[Customize a rendered application](../guides/customize-rendered-application.md).
