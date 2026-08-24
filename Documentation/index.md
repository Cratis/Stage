---
title: Stage
description: Compile Screenplay source, run modeled specifications, render reviewable Cratis code, or explore the supported runtime behavior in a disposable host.
---

Stage compiles folders of Screenplay (`.play`) files and provides three related but deliberately different paths:

1. **Renderer** — writes reviewable Cratis Arc + Chronicle backend code to disk.
2. **Runtime host** — performs the subset of modeled behavior Stage currently supports in a disposable sandbox.
3. **Specification runner** — checks modeled specifications and writes `results.json`.

These paths share the same compiled Screenplay syntax, but none should be mistaken for complete language coverage.
The renderer is the highest-priority path; the runtime and specification runner remain intentionally partial.

## What works today

### Renderer

`Cratis.Stage.Rendering.Cratis` now exposes a pure `CratisArtifactRenderPlanner` for the executable semantic model
(ESM). It accepts an immutable `ArtifactRenderRequest` and returns the complete in-memory `ArtifactRenderPlan`
before anything is written. Every artifact has a normalized relative path, exact bytes, and SHA-256 hash; errors
are typed diagnostics and make the plan non-publishable.

The first direct ESM capability is deliberately narrow and complete: concepts and composite types, one
`RegisterProject`-shaped command with `not empty` validation, its event destination and mappings, a one-instance
projection, an optional snapshot by-key query, and generated success/rejection specifications. The planner uses
stable semantic identities and materialized ESM mappings directly. It never converts the ESM back into
`ApplicationSyntax`, guesses a mapping, emits a `TODO`, or performs filesystem, process, network, clock, or ambient
environment access.

A caller supplies exact scaffold/template bytes as profile inputs:

```csharp
var scaffold = CratisArtifactRenderInput.CreateText(
    "Projects.csproj",
    "1.1.1",
    projectFileContent);
var profile = ArtifactRenderProfile.Create(
    CratisArtifactRenderPlanner.Target,
    "22.1.0",
    CratisArtifactRenderPlanner.Renderer,
    "1",
    [scaffold]);
var request = new ArtifactRenderRequest(
    semanticModel,
    executionPlan,
    profile,
    new(ArtifactRenderScopeKind.Application, semanticModel.Application.Id));

var plan = new CratisArtifactRenderPlanner().Plan(request);
if (!plan.Success)
{
    foreach (var diagnostic in plan.Diagnostics)
    {
        Console.Error.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
    }
}
```

Application, module, feature, and slice scopes produce the same paths and bytes for the artifacts they share.
Application scope also includes the fully resolved scaffold inputs. Unsupported reachable semantics block
publication rather than producing a thinner application.

The published syntax-based `IRenderer` remains available through an explicit compatibility adapter. That legacy
path still renders its existing broader `ApplicationSyntax` surface and writes directly to a target directory.
Direct writes do not provide managed staging or safe stale-file removal: after a legacy rendering failure, treat
the target as **unsafe and incomplete** and use a fresh target. Safe staged publication remains owned by CLI #101.

Screens, layouts, forms, and other frontend/UI artifacts remain outside this backend milestone.

### Runtime host

The `cratis/stage` container starts Arc, an in-memory Chronicle kernel, OpenAPI/Scalar, and the modeled API surface.
A runtime command evaluates its `produces` mappings, appends the resulting facts to Chronicle, and echoes its
payload in Arc's command result. Modeled command validation and authorization are not yet enforced by this runtime
path.

Queries currently fail closed. Stage does not yet receive an executable query authorization contract, so modeled
query performers deny access and return no data rather than exposing projected documents under invented semantics.

### Specification runner

`cratis/stage-specrunner` compiles the model, checks its declared specifications against the modeled facts and
expectations, writes `results.json`, and exits. This is model-level verification, not behavioral execution of every
slice through a live generated or runtime application.

## Curtain up

```bash
docker run --rm -p 9090:9090 -p 35000:35000 -v "$PWD":/eventmodel cratis/stage:latest
```

That mounts the current folder, compiles every `.play` file beneath it, and starts the sandbox. The model's API is
on `9090`; the Chronicle kernel and its direct Workbench endpoint are on `35000`.

The [Cratis CLI](/cli/reference/run) wraps this in `cratis run`, so you rarely type the `docker run` yourself.

## Packages and containers

| What                     | Image / package                 | Purpose                                                                                                          |
| ------------------------ | ------------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| The Stage host           | `cratis/stage`                  | Disposable partial runtime with Arc, Chronicle, OpenAPI, command fact appending, and fail-closed queries.        |
| The specification runner | `cratis/stage-specrunner`       | Run-to-completion model-level specification verification.                                                        |
| The renderer             | `Cratis.Stage.Rendering.Cratis` | Pure ESM artifact planning for the first complete backend vertical, plus the syntax-based compatibility renderer. |
| The contracts            | `Cratis.Stage.Contracts`        | Internal/tooling seams and specification results produced from compiled Screenplay syntax.                       |

## Where to go next

- [The Stage container](docker/index.md) — what is inside the image, how it boots, its ports, mount points, and configuration.
- [The specification runner](docker/spec-runner.md) — running a model's specifications as a container job.
- [URLs of a running Stage](reference/urls.md) — the runtime endpoints and their current behavior.
- [Render plans](reference/render-plans.md) — what Stage resolves for each `ui profile` a model ships, and what it reports when a target does not fully resolve.
