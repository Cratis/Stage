---
title: Stage
description: Hands an authored event model a stage and lets it perform — a live, running Cratis application materialized from Screenplay source at runtime, with no code generation and no build step.
---

A Screenplay file is a script, and a script isn't a show until someone performs it. Hand **Stage** a folder of
Screenplay (`.play`) files and it puts the model on its feet: commands and queries (Arc), read models and
projections (Chronicle), and an OpenAPI surface — all materialized **at runtime**. Nothing is generated to disk
and compiled. Change the model, restart, and the performance changes with it.

You consume Stage as two containers and a NuGet package:

| What | Image / package | Purpose |
|---|---|---|
| The Stage host | `cratis/stage` | A self-contained sandbox — Chronicle kernel, in-memory storage and the Stage engine in one container. Mount `.play` files, get a live API. |
| The specification runner | `cratis/stage-specrunner` | A run-to-completion job that runs the specifications in a model and writes `results.json`. |
| The contracts | `Cratis.Stage.Contracts` | The event model intermediate format, its serialization, and the specification result types — for tooling that produces or consumes a model. |

## Curtain up

```bash
docker run --rm -p 9090:9090 -p 35000:35000 -v "$PWD":/eventmodel cratis/stage:latest
```

That mounts the current folder, compiles every `.play` file beneath it, and starts the performance. Two ports
matter: the model's own API on `9090` and the Chronicle Workbench on `35000`.

The [Cratis CLI](/cli/reference/run) wraps exactly this in `cratis run`, so you rarely type the `docker run`
yourself.

## Where to go next

- [The Stage container](docker/index.md) — what is inside the image, how it boots, its ports, mount points and configuration.
- [The specification runner](docker/spec-runner.md) — running a model's specifications as a container job.
- [URLs of a running Stage](reference/urls.md) — every URL a play session exposes, and how the model's names turn into routes.
