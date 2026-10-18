---
title: Default generated Scene
description: Eligibility, input formats, result selection and proxy registration for the default Cratis application screen.
---

The Cratis application artifact planner uses an authored Scene unchanged when the profile carries one.
Otherwise it emits `scene.json` with command forms and eligible editable keyed lookups, plus `src/bindings.ts`.
It does not mutate the profile or generate per-screen TypeScript components.

Authored profiles can carry [slice-owned screen partitions](render-plans.md#slice-owned-screen-fragments) from the same compilation. Stage assembles the canonical app Scene from those records and emits each declaring slice's `Screens.scene.json`, including in scoped plans. Shared Scene aggregates are application-scope only. The synthetic default screen remains application-owned: it is not copied into every slice as that slice's UI.

## Navigation, toolbar buttons and template references

Three Screenplay constructs reach the Scene model through `ScreenplaySceneVisitor`. None of them is dropped, and the rules that judge them are Scene's, not Stage's.

**Navigation regions.** `navigation topbar|sidebar` in the first declared `layout` becomes the application's `NavigationRegion`s and its `ApplicationLogo`; a `module` block becomes a `ModuleNavigation` whose region carries the `NavigationRegionMode` (`inherits`, `extends`, `overrides`). A module without a block has no entry - Scene treats a missing region as inheriting, and its `EffectiveNavigation` computes the items, so Stage does not. An item keeps its id, title (a `$strings.<key>` token as written), icon, `slot`, nested items, and a `target` as a `DestinationReference` naming the module and the dotted feature path. `contribute to` is a separate mechanism and converts as before. An application that declares no region serializes without a `navigation` member, so its plan digest does not move.

**Toolbar buttons.** `button <id> title "..." slice <Slice> emphasis <e> icon <i>` becomes a `core:toolbarItem` of kind `Action` carrying `slice`, `emphasis` and `icon`, the shape Scene's `ToolbarButtons.FromItem` reads. The slice is written as the qualified id (`Module.Feature.Slice`) the structure knows, so a suffix such as `Orders.PlaceOrder` resolves the same way in the engine as in the compiler.

**Template references.** `template <Name>` and `template blueprint <Blueprint> <Name> [version "<range>"]` become `SceneApplication.TemplateReferences`, each a Scene `TemplateReference` (source, id, blueprint, version range) with the scope it was assigned at. The template's category and the slice kinds of a `scopes slice` template travel in its `TemplateMetadata`. An application with no assignment serializes without a `templateReferences` member.

**Sample data.** A top level `sample data <Name>` pack becomes a Scene `SampleDataPack` owned by `user`: its id is the lower-case, hyphenated form of the name (`NordicDemo` becomes `nordic-demo`), its title defaults to the name and its locale to Scene's default. Row fields keep their authored literal - text, whole number, decimal, Boolean or null - in authored order. The `sample data` block of a `ui profile` becomes the profile's `SampleDataSelection`: the active pack chosen once, shape mappings and read model bindings, every pack reference using the same derived id. `active blueprint <id>` keeps the blueprint's id as written and is resolved by the build against the packages the profile activates. Sample data is design and preview time only: it is never part of a production build and never reaches the semantic model. Whether the rows fit the read model is the Screenplay compiler's check; whether the pack and selection are well formed as Scene models them is Scene's, and a problem is reported as a `SampleDataInvalid` finding.

**Structure and findings.** The planner builds Scene's `ApplicationStructure` - modules, features and slices, each slice with its kind (a Translate slice is an automation: it has no user input) - from the compiled model and runs Scene's structure, navigation and toolbar validation over it. What they find is reported as `StructureInvalid`, `NavigationInvalid` and `ToolbarButtonInvalid` [findings](render-plans.md#findings), never thrown. The structure is a reference input; it is never written into the Scene document.

## Lookup eligibility

Backend semantic admission runs before composition. A default lookup requires all of the following:

- A modeled keyed query with optional-single (`ZeroOrOne`) snapshot semantics, not a collection.
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

The canonical external component uses `Cratis.Components:queryInputForm` with these properties:

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
ambiguous: both are registered, but neither gets a default lookup form. The current textual ESM v1 binder also
rejects duplicate query names; registration still preserves distinct identities in supplied semantic models.
Command registration and command form behavior are unchanged. Bindings and Scene JSON contain no HTTP route.

## Package and verification boundary

Stage's live frontend is an npm workspace root at `Source/Frontend`. The host consumes the runtime workspace
at `Source/Frontend/Runtime` as `@cratis/stage.runtime`; its React, Arc, Scene and Components dependencies are
peers pinned to the host's versions so contexts and registries are shared. `npm test --prefix Source/Frontend`
runs both host and runtime specifications, and `npm run build --prefix Source/Frontend` builds the package
before the host, keeping the host output at `Source/Frontend/dist` for the Stage image.
The runtime package is published at the Stage release version before NuGet publication. Generated applications
still receive the embedded runtime sources in this release; their output and dependency shape are unchanged.

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
