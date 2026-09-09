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
before anything is written. The plan carries an explicit artifact-schema version, and every artifact has a
normalized relative path, exact bytes, and SHA-256 hash; errors are typed diagnostics and make the plan
non-publishable.

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
Legacy inline command handlers support only C# (`csharp`) bodies proven not to bind to the generated `context`
parameter. Context-independent bodies such as `return Array.Empty<object>();` remain supported and are emitted
as authored, with the renderer's normal indentation. Screenplay and Arc have different command contexts; no
shared type-and-meaning mapping is assumed. Direct references, aliases, captures, `dynamic` aliases, and `nameof`
references to the parameter are rejected, including escaped and Unicode identifiers. Symbol-bound references to
other declarations, such as a shadowing lambda parameter or an anonymous object's `context` property, are not
references to the generated parameter.

Admission fails closed for non-C# languages, malformed or incomplete bodies, all preprocessor directives
(including inactive branches), analysis failures, and unresolved or ambiguous potential context bindings.
Unresolved unrelated generated event types do not by themselves block admission: this narrow binding check is
**not** a guarantee of arbitrary C# compilation, Screenplay-to-Arc semantic parity, or a security sandbox.
Declarative `produces` behavior is unchanged. File-backed command implementations have a separate guard below.

Every inline handler in a selected state-change slice is checked before emission, not only the first command;
`produces` is not a fallback for a rejected inline body. Rejection throws `UnsupportedInlineCommandHandler` with
code `STAGE-CRATIS-INLINE-001`, the command name, full slice path, exact authored code location, and reason.
The existing legacy failure flow skips that slice's artifact and specifications, continues independent output,
attempts an advisory failure marker, and ends with `RenderingFailed` rather than a success summary. Unselected
unsafe slices do not block a valid scoped render. The syntax compiler and ESM binder policies are unchanged.

Legacy file-backed command implementations are not supported. Every command in a selected state-change slice
is checked for `Handler.File` before inline admission or emission, including commands after the first. Rejection
throws `UnsupportedFileBackedCommandHandler` with code `STAGE-CRATIS-FILE-001`, the command name, full slice path,
exact command source location, and the symbolic file reference with its own source location. The fixed reason is
"file-backed command implementation not supported by legacy Cratis renderer", not a claim that the file is missing.
No implementation file is read or validated. File references remain valid Screenplay syntax and symbolic paths;
declaration file annotations are not handler implementations and do not trigger this guard.

This is a compatibility change: formerly ignored file implementations now block instead of silently emitting an
empty handler. Manually constructed syntax containing both a file and inline code or `produces` also blocks;
neither is a fallback. The DSL already rejects combining `handler` and `produces`. File rejection follows the same
existing partial-output failure flow described above. No supported external binding or replacement implementation
is supplied; migrate to declarative behavior only when it is equivalent. This narrow correction does not close
the broader Stage #13 file-backed implementation work or change other file implementation families.

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

- [Build a renderer target](guides/build-renderer-target.md) — implement a deterministic Screenplay-to-code target with the Stage planning contracts.
- [The Stage container](docker/index.md) — what is inside the image, how it boots, its ports, mount points, and configuration.
- [The specification runner](docker/spec-runner.md) — running a model's specifications as a container job.
- [URLs of a running Stage](reference/urls.md) — the runtime endpoints and their current behavior.
- [Scene UI render plans](reference/render-plans.md) — what Stage resolves for each `ui profile` a model ships, and what it reports when a target does not fully resolve.
