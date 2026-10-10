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

## Sources and identity

| `PlaySources` member | Meaning |
| --- | --- |
| `Root` | Required identity and attachment root, normally the repository's Screenplay directory. |
| `Paths` | Files and/or folders, absolute or relative to `Root`. Folders are searched recursively. Empty selects all of `Root`. |
| `CatalogPath` | Optional authoritative identity catalog, absolute or root-relative. |

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

Context-only sources need no flag: supply concepts, types, sibling features, and other
compile dependencies in `Paths`, but omit their scopes from `PlanSelection`. Referenced
shared declarations and required constraints can still appear in the output.

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
| Generated policies and runtime helpers | Application-root generated folders | Application-root namespace prefixes |

With root namespace `Acme.Shop`, domain `Sales`, and slice `PayOrder` in
`Orders/Checkout/Payments`, a command lives at
`Sales/Orders/Checkout/Payments/PayOrder/PayOrder.cs`, in namespace
`Acme.Shop.Sales.Orders.Checkout.Payments.PayOrder`. `Money` lives at
`Sales/Common/Money.cs`, in `Acme.Shop.Sales.Common`.

Empty domain preserves existing artifact bytes and paths. Common artifact bytes stay
identical across scopes using the same domain and source model.

### Application-root identity limitation

For v1, generated policy and typed-context files remain at the application root and
are keyed by semantic identity, not domain. Distinct domains under one application
must not reuse identical semantic operation identities. Changing only `Domain` does
not create new semantic identities or Chronicle event-type identities; it is placement,
not an isolation boundary.

## Scaffold-only mode

`PlanScaffold(options)` needs no sources. It emits the plain backend and frontend
scaffold at the application root, without a semantic revision or underlying
`ArtifactRenderPlan`. Domain must be empty. This mode does not compose a default Scene
or add model-specific strings wiring. The scaffold's pinned dependencies satisfy
Chronicle 19.30.0 and Arc.Chronicle 22.49.1 or later.

Plan the scaffold once per application. Every subsequent source or loaded-model scoped
plan excludes it, regardless of selection size. Studio can scaffold in one session,
then request feature plans in later sessions using the same application names and
Screenplay root.

## Publication check

Before writing a scoped plan, publishers must call:

```csharp
public static CratisPublicationCheck CheckPublication(
    ArtifactRenderPlan plan,
    Func<string, string?> readExistingFile);
```

This is a signature excerpt; `ArtifactRenderPlan` is in
`Cratis.Stage.Contracts.Rendering`. Pass the plan from `CratisRendering.Plan`,
or `CratisPlanResult.Plan` after a successful `PlanFrom` call. The reader receives
an application-root-relative path and returns existing text, or null for an absent
or unreadable file.

| Result | Publisher action |
| --- | --- |
| `CratisPublicationCheck.Compatible` | Continue with the publisher's normal safety checks. |
| `CratisPublicationCheck.RequiresApplicationScope` | Refuse the scoped publication or render the entire application first. `Paths` lists incompatible paths in ordinal order; `Reason` explains the migration requirement. |

The check reads only the legacy aggregate paths the plan would overwrite:
`GeneratedPolicies/Policies.cs`, `GeneratedPolicies/PolicyBodies.cs`, and
`TypedContexts/PolicyContext.cs`, at their planned placement. File presence alone
is not a legacy signal, because the split layout uses the same paths. Identical
planned content (after newline normalization) is compatible. Otherwise the check
recognizes the split registry and shared C# declarations, rejecting aggregate
`StagePolicy_*` classes, per-site bodies, `TypedContext_*` wrappers and unrecognized
content. Application-scope plans are always compatible, without destination reads.

A scaffold-only plan never contains policy files, so there is nothing to check.
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

| Code | Outcome |
| --- | --- |
| `STAGE-PLAN-001` | Source missing, not a `.play` file, invalid path, outside root, or containing a link or reparse point below root. |
| `STAGE-PLAN-002` | No `.play` files found. |
| `STAGE-PLAN-003` | Compilation failed; original `PLAY` diagnostics and locations accompany it. |
| `STAGE-PLAN-004` | Execution-plan admission refused. |
| `STAGE-PLAN-005` | Source or catalog could not be read. |
| `STAGE-PLAN-006` | Identity catalog invalid. |
| `STAGE-PLAN-010` | Selection empty. |
| `STAGE-PLAN-011` | Unknown module. |
| `STAGE-PLAN-012` | Unknown feature or invalid feature path. |
| `STAGE-PLAN-013` | Unknown slice. |
| `STAGE-PLAN-014` | Selection matches no slices. |
| `STAGE-PLAN-020` | Invalid domain segment. |
| `STAGE-PLAN-021` | Domain collides with a reserved root path. |
| `STAGE-PLAN-022` | Domain supplied for scaffold-only mode. |
| `STAGE-PLAN-030` | Invalid application, project, or namespace options. |

Existing renderer `STAGE-CRATIS`, `STAGE-ESM`, and authorization diagnostics pass through
unchanged. See [How the Stage renders a model](stage-rendering.md) for renderer admission.
