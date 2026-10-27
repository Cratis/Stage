---
title: Plan artifacts from Screenplay sources
description: Compile Screenplay files and plan named scopes or an application-root scaffold without publishing.
---

Use the experimental `CratisRendering.PlanFrom` facade to turn files and authored
names into a deterministic Cratis artifact plan. It compiles every supplied source,
then renders only the selected scopes. It does not publish files or run the application.
If you already hold semantic identities, the existing `CratisRendering.Plan` API
remains available with unchanged placement.

## API

All facade types are in `Cratis.Stage.Rendering.Cratis`. This excerpt assumes an
existing `screenplayRoot` directory and a `cancellationToken` supplied by your caller.

```csharp
using Cratis.Stage.Rendering.Cratis;

var options = new CratisPlanOptions("Shop", "Shop.Backend", "Acme.Shop");
var scaffold = CratisRendering.PlanScaffold(options);

var result = await CratisRendering.PlanFrom(
    new PlaySources(screenplayRoot, ["orders.play", "common"]),
    new PlanSelection([
        PlanSelectionEntry.Feature("Orders", "Checkout", "Payments"),
        PlanSelectionEntry.Slice("Orders", "Registration", "RegisterOrder")
    ]),
    options with { Domain = "Sales" },
    cancellationToken);

if (result.Success)
{
    // CheckPublication(result.Plan!, readExistingFile) must be Compatible before writing.
    // Publish result.Artifacts relative to the application root you own.
    // Stage does not choose or write that destination.
}
```

`PlanFrom(LoadedSemanticModel, PlanSelection, CratisPlanOptions)` performs the same
selection and rendering without I/O, for callers that already compiled their sources.

## Caller profile and byte parity

Set `CratisPlanOptions.Profile` to the complete `ArtifactRenderProfile` you use for a
full render. Build it with `CratisRendering.CreateProfile`, including any composed
Scene and companion strings catalog, then add authored documentation with
`CratisRendering.WithAuthoringMetadata` from the same compilation. Pass that profile
to either `PlanFrom` overload; keep its project and root namespace equal to the plan
options.

For example, given an existing `compilation`, `loaded` model, `selection`, optional
`scene`, and companion `stringsFiles` with a default locale of `en`:

```csharp
var options = new CratisPlanOptions("Shop", "Shop.Backend", "Acme.Shop");
var profile = CratisRendering.WithAuthoringMetadata(
    CratisRendering.CreateProfile(options.ApplicationName,
        new(options.ProjectName, options.RootNamespace), scene, stringsFiles, "en"),
    compilation);
var scoped = CratisRendering.PlanFrom(loaded, selection, options with { Profile = profile });
```

For the same compiled model, attachments, names, profile inputs, and domain, every
artifact in a successful scoped plan is byte-identical to the artifact at the same
path in a full application plan. Scoped plans still exclude application scaffolding
and application-only Scene and strings artifacts. A null `Profile` keeps the existing
defaults and bytes; Stage does not automatically add authored documentation or strings.

The caller profile uses the same admission as the public artifact planner. Invalid,
stale, duplicate, or unrecognized inputs and mismatched project or namespace names
produce `STAGE-CRATIS-001`, not a planning exception. The package-owned scaffold roster
cannot be replaced. Omit domain placement from the supplied profile and use `Domain`
instead: `PlanFrom` owns that input and refuses a caller-supplied domain input.
Pass the same `Profile` to `PlanScaffold` and `PlanFrom`. The scaffold supplies
`GeneratedStrings.cs` and configures `Program.cs` for its strings catalog, so scoped
code that references localized messages compiles with that scaffold. Documentation
is checked against the model by `PlanFrom`; scaffold planning does not consume it.

## Reducer rendering convention

Existing applications and all existing `CreateProfile` overloads remain **classic**:
Stage emits a separate `IReducerFor<T>` class. It does not infer a new application
from a missing file or a newer package version.

For a confirmed new application, select self-reducing models explicitly:

```csharp
var profile = CratisRendering.CreateNewApplicationProfile(
    "Shop", new("Shop.Backend", "Acme.Shop"));
var options = new CratisPlanOptions("Shop", "Shop.Backend", "Acme.Shop")
{
    Profile = profile
};
var scaffold = CratisRendering.PlanScaffold(options);
```

The scaffold includes application configuration at `.cratis/stage/rendering.json`:

```json
{"version":3,"application":"Shop","reducerConvention":"self","projectionConvention":"model-bound"}
```

On subsequent renders, the host reads that file and resolves it back into the profile:

```csharp
var input = ReducerConventionInput.Read("Shop", configurationJson);
var profile = CratisRendering.WithReducerConvention(
    CratisRendering.CreateProfile("Shop", new("Shop.Backend", "Acme.Shop")), input);
```

`configurationJson` is the file content supplied by the host. Stage does not search
the destination. `WithReducerConvention` also accepts a profile carrying Scene,
strings, and authoring metadata. Pass the resulting profile to both `PlanFrom`
overloads through `CratisPlanOptions.Profile`, and to scaffold planning.

The canonical input is named `cratis-rendering:reducer-convention`.
Its application name must match; its reducer convention must be `classic` or `self`.
Versions `1` and `2` retain fluent scoped projections and their existing bytes.
Version `3` requires `projectionConvention: "model-bound"`. It optionally carries the
same non-empty, sorted `readModels` overrides as version `2`; the canonical property
order is `version`, `application`, `reducerConvention`, `projectionConvention`, then
`readModels` when present.
`ReducerConventionInput.Read` refuses malformed, duplicate-field, unknown-version,
and unsupported configuration with `InvalidArtifactRenderContract`. Invalid inputs
submitted directly to planning produce `STAGE-CRATIS-001` and no artifacts.
The input is validated separately from the immutable scaffold roster. Its bytes,
version, and hash distinguish resolved profiles; an unused convention does not alter
a scoped plan's output digest. A persisted configuration artifact does affect the
scaffold or application plan digest.

Do not switch an existing classic application to `self` wholesale: every classic
reducer would get a new observer identity and its old file would remain. Migrate
existing read models one at a time instead (below). Existing CLI and Studio callers
that do not pass the input continue to render classic models.

### Model-bound scoped projections

New applications select version `3`. A From-only scoped projection with flat property
or event-source-id assignments renders `[FromEvent<T>]`, `[NoAutoMap]`, and explicit
`[SetFrom<T>]` or `[SetFromContext<T>]` attributes on its read model instead of a separate
`IProjectionFor<T>` class. Shapes not expressible by these attributes—including joins,
children, removals, every/all mappings, counters, nested paths, and composite keys—keep
the fluent renderer. Selecting this convention does not turn projections into reducers.

Existing applications can opt in by editing `.cratis/stage/rendering.json`: set
`version` to `3`, add `"projectionConvention":"model-bound"`, and keep their existing
`reducerConvention` and any `readModels` overrides. This changes eligible projections'
observer identity from the fluent projection type to the read model's full type name.
Plan a rebuild/replay and remove obsolete generated fluent projection files before
using the new output; Stage does not migrate projection observer state or remove those
files automatically. Keep version `1` or `2` to preserve existing projection identity.
Older Stage versions refuse version `3` configuration rather than silently ignoring it.

### Migrating a classic read model

Migration moves a read model built by one classic `IReducerFor<T>` reducer to a
self-reducing read model that keeps the old reducer's observer identity, so Chronicle
continues the same observer state without a replay. The host reads the existing reducer
file, derives evidence from its exact bytes, and adds the evidence to the profile:

```csharp
var evidence = ReducerMigrationEvidence.FromExistingFile(
    totalReadModelId, "Orders/Ordering/Totals/Fold.cs", existingFoldContent);
var profile = CratisRendering.WithReducerMigration(
    resolvedProfile, "Shop", [evidence], canRemoveFiles: true);
```

The evidence records the file path, the SHA-256 of its LF-normalized bytes, the observer
identity (the literal `[Reducer("…")]` identifier, or the reducer's full type name, with
`+` separating nested types), and the `IReducerFor<T>` read model type. An application-scope
plan then:

- renders the read model as self-reducing with `[Reducer("<previous observer id>")]`;
- omits the classic reducer file and lists it in `ArtifactRenderPlan.RequiredRemovals` with
  its expected hash and model;
- writes version `2` of `.cratis/stage/rendering.json`, which pins the read model:

```json
{"version":2,"application":"Shop","reducerConvention":"classic","readModels":[{"id":"sem1:…","convention":"self","observerId":"Shop.Orders.Ordering.Totals.Fold","previousPath":"Orders/Ordering/Totals/Fold.cs"}]}
```

Later renders read the version `2` file with `ReducerConventionInput.Read` and need no
evidence: they render identical bytes and plan no removals. Version `1` files keep their
exact bytes. Version `2` entries must be sorted ordinally by `id`, unique, use convention
`self`, and carry a nonblank observer id and a portable `.cs` path. Other read models,
including others in the same slice, stay classic.

Migration is one way: Stage has no reverse migration. Removing an entry from the persisted
configuration only stops pinning the read model; the next plan renders it by the
application-wide convention as if it had never been migrated, without evidence, removals or
any guarantee that Chronicle keeps the observer state.

A persisted entry goes stale when its read model is renamed, removed, or no longer built by
exactly one reducer; every later plan then refuses with `STAGE-REDUCER-001`. To recover,
remove that entry from `readModels` in `.cratis/stage/rendering.json` (keep the remaining
entries in ordinal `id` order, and use version `1` when none remain) and plan again.

| Code | Refusal |
| --- | --- |
| `STAGE-REDUCER-001` | A persisted override or evidence names something that is not a read model built by exactly one reducer. |
| `STAGE-REDUCER-002` | The evidence does not match the model: the path is not where the classic reducer renders, or the observer identity or read model type differs. Also refused when the classic reducer's attribute sets more than its identifier (such as `eventSequence:` or `isActive:`), or the class carries `[EventSequence]` or `[EventStore]`, because the self-reducing read model would observe differently. |
| `STAGE-REDUCER-003` | The host did not declare that it can remove files. |
| `STAGE-REDUCER-004` | The plan is not an application-scope plan. Migration persists configuration, so it requires one. |
| `STAGE-REDUCER-005` | Publication check: a required removal no longer matches its expected hash, or a migrated read model's previous reducer file would remain without a removal. |

A refused plan has no artifacts and no removals. Self-mode member collisions still
refuse with `STAGE-ESM-019`; Stage never falls back to the classic reducer.

A plan carrying any error diagnostic plans no removals. This includes a plan rendered
around refused specifications: it still writes the other artifacts, but its
`RequiredRemovals` is empty, so the publication check reports the leftover classic reducer
as `UnsafeRemovals` and publication is refused until the refusal is resolved.

`FromExistingFile` throws `InvalidArtifactRenderContract` when the file does not parse, does
not declare exactly one `IReducerFor<T>` class, declares more than one reducer attribute, or
uses a non-literal identifier. Evidence can only be created through `FromExistingFile`.

## Sources and identity

| `PlaySources` member | Meaning |
| --- | --- |
| `Root` | Required identity and attachment root, normally the repository's Screenplay directory. |
| `Paths` | Files and/or folders, absolute or relative to `Root`. Folders are searched recursively. Empty selects all of `Root`. |
| `CatalogPath` | Optional canonical bare identity catalog or MCP persisted identity state (`cratis.screenplay.mcp-identities`, version 1), absolute or root-relative. |

Files must exist, have a `.play` extension, and remain under `Root`. Symbolic links
and reparse points below `Root` are refused, even when their targets are inside it.
This includes linked files, linked directories, and paths containing a link before
`..`. Folder searches report directory links without following them. `Root` itself
may sit under a linked mount. Duplicate files listed individually and through folders
are compiled once. Root-relative paths are
sorted ordinally, so listing order does not change identities, output bytes, or digest.

**Keep `Root` stable between calls.** Root-relative document paths determine document
identity and attachment resolution. Changing the root can change semantic identities
even when source text is unchanged. Setting it to a single file's parent matches the
existing single-file loader's identity behavior.

For a Screenplay MCP model root, set `CatalogPath` to `.screenplay/identities.json`.
Stage reads the wrapper through Screenplay's `PersistedIdentityStateSerializer`, not
as a bare catalog. Both formats require canonical bytes. Persisted document mappings
retain identities after a document moves; bare catalogs use path-derived document keys.
Omitting `CatalogPath` keeps the existing provisional-identity behavior: Stage does not
automatically discover the identity file.

Context-only sources need no flag: supply concepts, types, sibling features, and other
compile dependencies in `Paths`, but omit their scopes from `PlanSelection`. Referenced
shared declarations and required constraints can still appear in the output.

## Admitted release revisions

When `CatalogPath` names the root's MCP `.screenplay/identities.json`, Stage opens
that root through Screenplay's `ScreenplayModelRoot.OpenForReleaseBaselineCheck`.
Whole-root loading uses the returned workspace compilation and assesses that same
workspace with `StageReleaseBaseline.Check(workspace, baselineRead)`. Supply the
persisted application name and all root sources to obtain a whole-root assessment.
If a partial source set compiles, its assessment is unavailable rather than borrowed
from another revision. Screenplay 4.159.0 rejects stale catalog assignments during
compilation, including removed declarations and subsets that omit assigned documents;
Stage forwards that compilation failure without migrating identities.

The model root contains the sources Screenplay reads, which excludes `.git`, `.ai-work`, `bin`, `obj` and
`node_modules`. `.play` files found inside those folders while Stage walks a selected folder are left out,
the same way `screenplay baseline check` leaves them out. A file you name explicitly that the model root does not
read is refused with `STAGE-PLAN-001` and is never silently dropped.

A pending recovery operation, conflicting document mappings, or unreadable root
refuses loading with `STAGE-BASELINE-004`; an identity-state conflict reported by
Screenplay's root opener uses `STAGE-BASELINE-003`. Invalid catalog bytes detected
before opening use `STAGE-PLAN-006`. These refusals never recover or rewrite the root.

Bare catalogs and identity wrappers outside the root's metadata path keep the
compilation-based assessment with `ReleaseBaselineStore.Read`. Source loading never
admits a release or writes sidecars. An absent baseline adds no warnings. A present
baseline without authoritative identities reports an unavailable check, not guessed
changed or missing revisions. See [Screenplay's public model-root readers](/screenplay/tool/#call-these-from-your-own-tooling).

`LoadedSemanticModel.ReleaseBaseline` carries the assessment of the whole compiled
application. Both `PlanFrom` overloads forward it, including violations in sibling
slices outside the selected scopes. A manually constructed loaded model defaults to
`NotSupplied` and remains unchecked. For an in-memory workspace, compute the
assessment with `StageReleaseBaseline.Check(workspace, baselineRead)`; a
`SemanticCompilation` overload reconstructs its workspace and requires semantic-revision
parity. These types are in `Cratis.Stage.Contracts.Semantics`.

| Warning | Meaning |
| --- | --- |
| `STAGE-BASELINE-001` | An admitted event revision changed; includes contract identity, revision, stored name, display hint, admitted release, and first differing schema path when available. |
| `STAGE-BASELINE-002` | An admitted event revision is missing. |
| `STAGE-BASELINE-003` | The baseline is unreadable, has an unsupported schema, or belongs to another application. |
| `STAGE-BASELINE-004` | The check is unavailable, or the assessment belongs to a different semantic revision than the planned model. |

Warnings alone keep `Success` true and preserve every artifact byte. They contribute
to the plan digest because diagnostics are part of that digest. Blocking renderer
errors still refuse the plan. Use `screenplay baseline check <root>` as your CI gate:
Stage warnings do not enforce frozen revisions or Chronicle schema compatibility.
The baseline comes from an explicit `screenplay baseline admit` release step, not
rendering. See [Screenplay's baseline commands](/screenplay/tool/#admit-and-check-released-event-revisions).

Baseline checking is independent of `CheckPublication`: warnings do not make a
compatible destination incompatible, and they never waive `STAGE-EVENT-002`.

## Selection

Names match authored names exactly and ordinally, not their generated PascalCase
forms. Every entry includes a module. Use these path shapes:

| Factory | Path |
| --- | --- |
| `Module(name)` | Module |
| `Feature(params string[] path)` | Module / Feature / optional SubFeature … |
| `Slice(params string[] path)` | Module / Feature / optional SubFeature … / Slice |

Entries form a union; entry order, duplicates, and overlaps do not duplicate artifacts.
A feature includes all nested features. Unknown names fail the whole request with a
typed diagnostic naming the first unmatched segment and its known siblings in ordinal
order. Empty selections and selections containing no slices are refused.

## Options and placement

`ApplicationName`, `ProjectName`, and `RootNamespace` are required dot-separated C#
identifiers. They belong to the application, not an individual domain. The application
name must match an already loaded model. `Domain` defaults to empty; it is a relative
slash-separated sub-path such as `Sales` or `Sales/Retail`. Segments are PascalCase
normalized consistently with slice names. Traversal, empty segments, and invalid names
are refused. The first segment cannot collide with scaffold paths or the reserved
`Customizations`, `GeneratedPolicies`, `TypedContexts`, `GeneratedCommands`, or
`GeneratedTenancy` folders.

| Artifact | Relative path | Namespace |
| --- | --- | --- |
| Slice | `{Domain}/{Module}/{Feature}/{SubFeature…}/{Slice}/{File}.cs` | `{RootNamespace}.{Domain}.{Module}.{Feature}.{SubFeature…}.{Slice}` |
| Concept or composite type | `{Domain}/Common/{Name}.cs` | `{RootNamespace}.{Domain}.Common` |
| Scaffold | Application root | `RootNamespace` |
| Operation policies and opaque-policy bodies | `{Domain}/GeneratedPolicies/{File}.cs` | `{RootNamespace}.{Domain}.GeneratedPolicies` |
| Policy and reducer use-site wrappers | `{Domain}/TypedContexts/{File}.cs` | `{RootNamespace}.{Domain}.TypedContexts` |
| Shared registries and runtime helpers | Application-root generated folders | Application-root namespace prefixes |

With root namespace `Acme.Shop`, domain `Sales`, and slice `PayOrder` in
`Orders/Checkout/Payments`, a command lives at
`Sales/Orders/Checkout/Payments/PayOrder/PayOrder.cs`, in namespace
`Acme.Shop.Sales.Orders.Checkout.Payments.PayOrder`. `Money` lives at
`Sales/Common/Money.cs`, in `Acme.Shop.Sales.Common`.

Empty domain preserves existing artifact bytes and paths. Common artifact bytes stay
identical across scopes using the same domain and source model.

Operation policy, opaque-policy body, and typed-context wrapper files carry the domain
in their paths and namespaces. Arc policy names also include the normalized domain,
and each operation's authorization attribute uses that registered name. Separate
per-domain plans can therefore reuse operation identities without overwriting each
other's authorization. Shared `Policies.cs`, `PolicyBodies.cs`, `PolicyContext.cs`,
stream-id, command-receipt-time, and tenant-translation helpers remain at the application
root; their bytes do not depend on the selected domain.

Chronicle event-type ids remain application-wide. Changing `Domain` does not create
new event-type identities or an isolation boundary. Domains must still avoid duplicate
event-type ids; identity evolution is tracked by
[Screenplay #71](https://github.com/Cratis/Screenplay/issues/71).

## Scaffold-only mode

`PlanScaffold(options)` needs no sources. It emits the backend and frontend scaffold
at the application root, without a semantic revision or underlying `ArtifactRenderPlan`.
Domain must be empty. Pass the same `Profile` as subsequent `PlanFrom` calls: a strings
catalog adds `GeneratedStrings.cs` and the same request-localization wiring in
`Program.cs` as an application-scope render. Without a profile, scaffold bytes are
unchanged. The scaffold's pinned dependencies satisfy Chronicle 19.30.0 and
Arc.Chronicle 22.49.1 or later.

### Repository-root option

`RepositoryRoot` defaults to `false`. Set it to `true` only when the application root is the
repository root; Stage plans without touching the file system, so the caller decides. The scaffold
then also contains `.github/workflows/build.yml` and `.github/dependabot.yml`, and its README names
npm instead of Yarn for the frontend. The workflow has a `backend` job (restore, Debug build,
specifications, Release build) and a `frontend` job (generate the Arc proxies, `npm install`,
`npm run build`). It runs on pushes to `main`, pull requests and manual dispatch. Actions are pinned
by full commit hash, and Dependabot updates them weekly. Application-scope plans include these files;
scoped plans do not. Pass the same value, together with the same `Profile`, to every planning call:
a profile and options that disagree are refused with `STAGE-CRATIS-001`.

### File header option

Generated code belongs to the application, so by default no generated file carries a license or
copyright header. Set `FileHeader` to the plain-text lines every generated code file should start
with:

```csharp
var options = new CratisPlanOptions("Shop", "Shop", "Shop")
{
    FileHeader = ["Copyright (c) Acme Corporation. All rights reserved.", "", "Proprietary and confidential."]
};
```

Stage writes each line as a line comment in the file's language, followed by one blank line.
An empty or whitespace-only line becomes just the comment prefix. Trailing whitespace is trimmed.
Do not add comment markers yourself. `ArtifactCommentSyntaxes.Default` defines the core table:

| Extensions | Prefix | Refused sequences |
| --- | --- | --- |
| `.cs`, `.ts`, `.tsx`, `.mts`, `.cts`, `.js`, `.jsx`, `.mjs`, `.cjs`, `.kt`, `.kts`, `.go` | `//` | None |
| `.java` | `//` | Every ASCII `\u` sequence, including repeated-u escapes |

JSON, CSS, HTML, Markdown, YAML and MSBuild files never get a header. A caller can supply a
different table through `ArtifactRenderRequest.CommentSyntaxes`. This is a core contract;
it does not enable canonical header admission in the Axon target, which needs a separate update.

Lines must be non-null, valid UTF-16 and contain no line breaks, control characters (except tab),
Unicode line or paragraph separators, private-use characters, or bidi controls (U+061C, U+200E,
U+200F, U+202A–U+202E, U+2066–U+2069). Unsafe options give `STAGE-PLAN-023`.

The canonical hashed input is `stage:file-header`, version `1`, created with
`ArtifactFileHeader.CreateInput` or `ArtifactFileHeader.With`. Its bytes are the canonical JSON
string array. The core also reads `cratis-render:file-header` and `axon-render:file-header`;
Cratis option factories retain the legacy name for compatibility. Only one header input is allowed.
Malformed inputs give `STAGE-HEADER-001`; a line unsafe in a file's comment syntax gives
`STAGE-HEADER-002`. Either refuses the plan without artifacts, implementation gaps or removals.
The header changes code bytes and their plan digest and applies to the scaffold, scoped plans
and application-scope plans alike. `CratisRenderingOptions.FileHeader` sets it for `CratisRendering.Plan(...)` and
`CreateProfile(...)`. Pass the same lines to every planning call; a caller profile that already
carries a header combined with a non-empty `FileHeader` option is refused with `STAGE-CRATIS-001`.

### User interface option

A host that generates an application feature by feature needs a working frontend after every
generation, not only after a whole-application render. The application's shared UI is four files:
`scene.json` (the Scene composition), `src/bindings.ts` (which imports the generated Arc proxies),
`src/stage-scene.json` and `src/stage.ts` (what the emitted Stage runtime renders). The bindings import
a proxy from the folder of the slice that declares it, and Arc only generates that proxy when the
slice's backend code is built. Composing the shared UI over the whole model before every slice is
generated would import proxies that do not exist, and the frontend would not compile.

Set `UserInterface` to the slices whose generated code is already present in the application:

```csharp
var options = new CratisPlanOptions("Shop", "Shop", "Shop")
{
    UserInterface = new([PlanSelectionEntry.Feature("Catalog", "Products")])
};
var plan = CratisRendering.PlanFrom(loaded, new([PlanSelectionEntry.Slice("Ordering", "Checkout", "PlaceOrder")]), options);
```

The scoped plan then also emits the four shared UI files, composed over exactly those slices and the
plan's own selection. Entries use the same module, feature and slice paths as the selection; an
unknown entry is refused with the selection diagnostic, prefixed `User interface selection:`. An
empty `new([])` composes over the plan's own selection only. The default, `null`, keeps scoped plans
free of shared UI and reports `STAGE-SCREEN-006` when screen partitions are carried.

What the files contain depends on the profile:

- With authored screens carried as screen partitions, `scene.json` holds the authored screens declared
  by the present slices.
- Without an authored Scene, Stage composes the default screen over the present slices' commands and
  queries, the same way an application-scope plan composes it over every slice.
- An authored Scene that is not partitioned is the caller's whole composition and is carried as is.

When every slice of the model is present, the four files are byte-identical to the ones an
application-scope plan emits. The set is written to the profile as the hashed
`cratis-render:user-interface` input (a canonical JSON array of slice identities), so it contributes
to the plan digest. Pass the set that matches the application's state to each generation; a caller
profile that already carries the input combined with the option is refused with `STAGE-CRATIS-001`.
Scaffold files are not affected: scoped plans still exclude them.

#### An application generated one domain at a time

The four shared UI files sit at the application root, but a plan places its slices, and with them
their proxies, under its own `Domain` folder. A host that generates an application one domain at a
time therefore names where the other domains' slices were generated. Set `UserInterfaceDomains` to
those domains, each with the slices whose code is present under it, and keep `UserInterface` for the
slices present under the plan's own domain:

```csharp
var options = new CratisPlanOptions("Shop", "Shop", "Shop")
{
    Domain = "Support",
    UserInterface = new([]),
    UserInterfaceDomains = [new("Sales", new([PlanSelectionEntry.Module("Catalog")]))]
};
var plan = CratisRendering.PlanFrom(loaded, new([PlanSelectionEntry.Module("Tickets")]), options);
```

The shared UI then composes the screens of every named domain and imports each proxy from the folder
it was generated under, here `../Sales/Catalog/...` and `../Support/Tickets/...`. Planning the Sales
domain against the same state, with Support named in `UserInterfaceDomains`, emits the same four files
byte for byte, so generating one domain never drops another domain's UI. A slice this plan selects is
always placed under the plan's own domain.

The domains travel as the hashed `cratis-render:user-interface-domains` input (a canonical JSON object
of normalized domain paths, each holding its slice identities), which is added only when the option is
not empty; plans without it are unchanged. The option is refused with `STAGE-PLAN-024` without
`UserInterface`, `STAGE-PLAN-025` for an invalid or reserved domain, `STAGE-PLAN-026` for the plan's own
domain or a domain named twice, and `STAGE-PLAN-027` for a slice named under two domains. An unknown
entry is refused with the selection diagnostic, prefixed `User interface domain '<domain>':`.

Without this option, Scene composition, bindings, and screen runtime artifacts come only from an
application-scope render ([#275](https://github.com/Cratis/Stage/issues/275)).
A scaffold profile carrying a Scene emits no Scene artifacts and reports the
`STAGE-PLAN-031` information diagnostic. This mode does not compose a default Scene.

Plan the scaffold once per application. Every subsequent source or loaded-model scoped
plan excludes it, regardless of selection size. Studio can scaffold in one session,
then request feature plans in later sessions using the same application names and
Screenplay root. Scaffold files belong to the user after first run; an application-scope
plan includes them again, so publishers must preserve that ownership before overwriting.

The backend scaffold contains:

| Artifact | Purpose |
| --- | --- |
| `{Project}.csproj` | Web host, production dependencies, proxy-generation settings, Debug-only test packages, and the optional `Customizations/Dependencies.props` import. |
| `{Project}.slnx` | Solution containing the application project. |
| `Directory.Build.props` | Target framework from the profile, latest C# language version, nullable references, implicit usings, Arc read protection `On`, compiler/analyzer warnings as errors, and build-time code style. |
| `Directory.Build.targets` | Local boundary preventing parent build targets from leaking into the application. |
| `Directory.Packages.props` | Central NuGet package versions with transitive pinning enabled. |
| `.editorconfig` | Root C# formatting and style conventions, with BDD naming exceptions for specifications. |
| `README.md` | Prerequisites, local backend/frontend commands, specification execution, and customization guidance. |
| `Program.cs`, `GeneratedPolicyRegistration.cs` | Application startup and generated policy registration. |
| `appsettings.json`, `docker-compose.yml` | Development connection settings and pinned Chronicle image with named data volumes. |

Command, projection, query, and reaction specifications stay beside their slices, and there
is no separate specifications project. In Debug the application project is a test project: it
references the testing packages and compiles every `when_*.cs` file and every file under a
`when_*/`, `for_*/` or `given/` folder, so hand-written specifications follow the same
convention. Run them with `dotnet test {Project}.csproj -c Debug`. Release removes those files
and references no testing package. Because the package set differs, switching between Debug and
Release restores again; a Release build after a Debug restore must not pass `--no-restore`. Frontend scaffold files remain at their
existing paths, including `package.json` and `.frontend/`.

Builds explicitly include the application's `.editorconfig` and disable automatic
`.editorconfig` and `.globalconfig` discovery, so ancestor repository analyzer settings
do not leak into the scaffold. Customize the application's own `.editorconfig` to change
its build-time rules; specification paths permit Cratis BDD names.

Environment-dependent restore diagnostics stay visible as warnings: `NU1507` (multiple
package sources without source mapping) and `NU1900`–`NU1904` (audit availability and
package advisories). They do not fail the build; other warnings, including dependency
resolution warnings, remain errors.

All scaffold `PackageReference` entries are unversioned. After first run, bump NuGet
versions in the user-owned `Directory.Packages.props`, not individual project references.
Keep Chronicle client, ASP.NET Core integration, and testing versions aligned with the
Chronicle development image in `docker-compose.yml`. Update frontend versions in
`package.json`. Additional dependencies can use a central `PackageVersion` or
`VersionOverride` through the [customization import](../guides/customize-rendered-application.md#add-project-and-package-references).
No third-party analyzer packages, CI workflow, devcontainer, or Aspire project is emitted.

## Publication check

Before writing a plan at any scope, publishers must call:

```csharp
public static CratisPublicationCheck CheckPublication(
    ArtifactRenderPlan plan,
    Func<string, string?> readExistingFile);
```

This is a signature excerpt; `ArtifactRenderPlan` is in
`Cratis.Stage.Contracts.Rendering`. Pass the plan from `CratisRendering.Plan`,
or `CratisPlanResult.Plan` after a successful `PlanFrom` call. The reader receives
an application-root-relative path and returns existing text, or null only when the file
is confirmed absent. A reader that cannot read a file must throw rather than return null,
so the check never treats an unreadable destination file as absent.

| Result | Publisher action |
| --- | --- |
| `CratisPublicationCheck.Compatible` | Continue with the publisher's normal safety checks. |
| `CratisPublicationCheck.RequiresApplicationScope` | Refuse the scoped publication or render the entire application first. `Paths` lists incompatible paths in ordinal order; `Reason` explains the migration requirement. |
| `CratisPublicationCheck.ChangesEventTypeIds` | Refuse publication at every scope and surface the `STAGE-EVENT-002` error diagnostics. `Changes` lists path, qualified type name, existing id and planned id in ordinal path/type order. Pin the existing id, add a new event, or write an event type migration before replanning. |
| `CratisPublicationCheck.UnsafeRemovals` | Refuse publication at every scope and surface the `STAGE-REDUCER-005` error diagnostics. `Removals` lists each removal that no longer matches its expected hash or that the plan is missing for a migrated read model's previous reducer file. Restore or re-read the file and replan with fresh evidence. |

Scoped plans check policy layouts first and return `RequiresApplicationScope` when needed,
preserving the refusal understood by existing publishers. Otherwise event identities are
checked at every scope, including application scope. The check reads planned `.cs` paths
containing an `[EventType]` declaration and matches simple type identifiers within each path,
using canonical namespace-qualified names only to break ties and report changes. An argument-free or empty-string id falls back to the class
name. A nonliteral existing id or existing parse errors refuse publication because its
identity cannot be read safely. An unpinned rename that replaces one class with another
cannot be detected; use Screenplay's workspace/MCP rename, which pins by default.
Event files at paths the plan does not overwrite are not checked.

Scoped plans also read the legacy aggregate paths they would overwrite:
`GeneratedPolicies/Policies.cs`, `GeneratedPolicies/PolicyBodies.cs`, and
`TypedContexts/PolicyContext.cs`, at their planned placement. File presence alone
is not a legacy signal, because the split layout uses the same paths. Identical
planned content (after newline normalization) is compatible. Otherwise the check
recognizes the split registry and shared C# declarations, rejecting aggregate
`StagePolicy_*` classes, per-site bodies, `TypedContext_*` wrappers and unrecognized
content. Application-scope plans skip this policy-layout check, but still read event destinations.
`STAGE-EVENT-002` is enforced only by publishers that refuse every non-`Compatible`
result before writing any artifacts. Existing `cratis render` versions check only scoped
plans and refuse only `RequiresApplicationScope`; they do not enforce `STAGE-EVENT-002`.
The CLI integration is tracked in [Cratis/cli#354](https://github.com/Cratis/cli/issues/354).

A scaffold-only plan never contains event or policy files, so there is nothing to check.
`PlanScaffold` has no underlying `ArtifactRenderPlan`. Publication checks do not
alter planning or replace destination ownership, concurrency, recovery or write-failure
checks.

`ArtifactRenderPlan.Scope` and `AdditionalScopes` retain the requested selection.
They are metadata, excluded from the output-only digest.

## Results and digest

`CratisPlanResult` contains sorted `Artifacts`, typed `Diagnostics`, `Digest`,
`ApplicationName`, `TargetVersion`, optional `Revision`, and optional `Plan`.
`Success` means no diagnostic has error severity; expected failures return diagnostics.
Cancellation and inconsistent render contracts throw.

The digest is lowercase SHA-256 over the artifact-plan schema, target and renderer
identities and versions, application name, ordered artifact paths/kinds/content hashes
and semantic sources, and ordered diagnostics. It excludes the semantic revision and
filesystem destination. Source diagnostic locations contribute to failure digests.
`ArtifactRenderPlan.Digest` and scaffold-only results use the same computation. Revision
is exposed separately: equal output digests do not promise equal input revisions.

## Diagnostics

Semantic source loads retain every diagnostic in `SemanticModelLoadResult.Diagnostics`,
with its original severity and order. `Notes` derives the non-blocking Information and
Warning diagnostics from that collection; `Errors` derives the blocking diagnostics.
Successful loads also retain notes on
`LoadedSemanticModel.Notes`; throwing loads retain them on `InvalidSemanticModel.Notes`
without including them in the exception's blocking diagnostics or message. Each note
carries `Code`, `Message`, `Source` (root-relative path, line and column) and `Severity`.

| Code | Outcome |
| --- | --- |
| `STAGE-PLAN-001` | Source missing, not a `.play` file, invalid path, outside root, or containing a link or reparse point below root. |
| `STAGE-PLAN-002` | No `.play` files found. |
| `STAGE-PLAN-003` | Compilation failed; original `PLAY` diagnostics and locations accompany it. |
| `STAGE-PLAN-004` | Execution-plan admission refused. |
| `STAGE-PLAN-005` | Source or catalog could not be read. |
| `STAGE-PLAN-006` | Identity catalog or persisted MCP identity state invalid. |
| `STAGE-PLAN-010` | Selection empty. |
| `STAGE-PLAN-011` | Unknown module. |
| `STAGE-PLAN-012` | Unknown feature or invalid feature path. |
| `STAGE-PLAN-013` | Unknown slice. |
| `STAGE-PLAN-014` | Selection matches no slices. |
| `STAGE-PLAN-020` | Invalid domain segment. |
| `STAGE-PLAN-021` | Domain collides with a reserved root path. |
| `STAGE-PLAN-022` | Domain supplied for scaffold-only mode. |
| `STAGE-PLAN-023` | A `FileHeader` line is null, malformed UTF-16, or contains a line break, control, private-use or bidi formatting character. |
| `STAGE-HEADER-001` | Malformed, unsafe, noncanonical or duplicate header input. |
| `STAGE-HEADER-002` | Header text cannot safely be represented in an artifact's comment syntax (for example Java `\u`). |
| `STAGE-PLAN-024` | `UserInterfaceDomains` is set without `UserInterface`. |
| `STAGE-PLAN-025` | A user interface domain is invalid or collides with a reserved root path. |
| `STAGE-PLAN-026` | A user interface domain is the plan's own domain or is named twice. |
| `STAGE-PLAN-027` | A slice is named under two user interface domains. |
| `STAGE-PLAN-028` | `ServiceLayout` is combined with `RepositoryRoot`. See [Multi-service repository layout](stage-rendering.md#multi-service-repository-layout). |
| `STAGE-PLAN-029` | Invalid `PlanRepository` options. |
| `STAGE-PLAN-030` | Invalid application, project, or namespace options. |
| `STAGE-PLAN-031` | Information: a scaffold profile carries Scene input; Scene artifacts are produced only by application-scope plans. |

Existing renderer `STAGE-CRATIS`, `STAGE-ESM`, and authorization diagnostics pass through
unchanged. See [How the Stage renders a model](stage-rendering.md) for renderer admission.
