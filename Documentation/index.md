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

`Cratis.Stage.Rendering.Cratis` renders concepts, types, state changes, state views, reactions, specifications, and
the authorization declarations it can express exactly with Arc attributes. Role-only alternatives render as
`[Roles(...)]`, authenticated-only access renders as `[Authorize]`, and an absent declaration renders as
`[AllowAnonymous]`. Every generated query method carries its own exact attribute; Stage does not union distinct
query policies on the read model.

Conjunctions, claims, authored policy code, missing policies, and trees mixing supported and unsupported
requirements raise `STAGE-AUTH-001` for the containing artifact. Stage continues independent artifacts to collect
diagnostics, then faults the operation with `RenderingFailed`; an unqualified `Rendering complete.` is success
only.

Filesystem output is currently direct-write, without managed staging or safe stale-file removal. After any failure,
treat the target as **unsafe and incomplete**: a file from an earlier run can remain physically present even when
the current run blocks that artifact. Stage attempts to create an advisory `.stage-render-failed` marker without
overwriting an existing file, but the marker neither disables nor deletes stale code. Use a fresh target after a
failure. Managed `ArtifactRenderPlan` commit semantics are deferred to Stage #56 and CLI #101.

Screens, layouts, forms, and other frontend/UI artifacts are not yet rendered, and other unsupported model
constructs are reported as diagnostics.

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
| The renderer             | `Cratis.Stage.Rendering.Cratis` | Reviewable backend generation with exact per-method query authorization and explicit failed-operation signaling. |
| The contracts            | `Cratis.Stage.Contracts`        | Internal/tooling seams and specification results produced from compiled Screenplay syntax.                       |

## Where to go next

- [The Stage container](docker/index.md) — what is inside the image, how it boots, its ports, mount points, and configuration.
- [The specification runner](docker/spec-runner.md) — running a model's specifications as a container job.
- [URLs of a running Stage](reference/urls.md) — the runtime endpoints and their current behavior.
- [Render plans](reference/render-plans.md) — what Stage resolves for each `ui profile` a model ships, and what it reports when a target does not fully resolve.
