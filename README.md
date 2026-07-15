# Cratis Stage

Stage takes an authored event model — the intermediate JSON format produced by [Cratis Studio](https://github.com/Cratis/Studio)
or any other tool — and materializes it into a **live, running Cratis application at runtime**: dynamically generated
commands and queries (Arc), read models and projections (Chronicle), and a Scalar/OpenAPI surface. No code generation,
no compilation — the model *is* the application.

Stage is self contained: it has no dependency on Studio. It is consumed as containers (the host and the
specification runner) and as a NuGet package (the contracts) from Studio, the Cratis CLI, or your own tooling.

## Projects

| Project | Package / Image | Purpose |
|---|---|---|
| `Source/Contracts` | `Cratis.Stage.Contracts` (NuGet) | The event model intermediate format (with its embedded JSON schema), specification run results, and the serialization for both (`EventModelFile`, `SpecificationRunResultsFile`, `StageJson`). |
| `Source/Stage` | `Cratis.Stage` (NuGet) | The engine — synthesizes commands, queries, validators, read models, and projections from a deserialized `EventModel` at runtime. |
| `Source/Host` | `cratis/stage` (Docker) | Self-contained play sandbox: MongoDB + Chronicle kernel + the Stage engine in one container. Mount a model at `/eventmodel`, get a live API on port `9090`. |
| `Source/SpecRunner` | `cratis/stage-specrunner` (Docker) | Run-to-completion job that loads an event model from the mounted `/model` folder, runs its specifications and writes `results.json` to the mounted `/output` folder. |

## Consuming

Everything Stage needs as input is a serialized `EventModel` — the types and the exact serialization live in
`Cratis.Stage.Contracts`, so consumers write the file with `EventModelFile.Write(model)` and never hand-roll JSON.

```
Studio / CLI ──(event-model.json)──▶ cratis/stage            ⇒ live HTTP API (port 9090)
Studio / CLI ──(event-model.json)──▶ cratis/stage-specrunner ⇒ results.json
```

- The **host** takes the model file path as its first argument (the container entrypoint finds it in `/eventmodel`).
  Deployment configuration can be supplied through a dedicated `cratis-stage.json` file (path overridable with the
  `STAGE_CONFIG` environment variable) instead of `appsettings.json`.
- The **spec runner** takes `--model <file>` and `--output <file>`, with optional `--slice <guid>` / `--spec <guid>`
  filters; the container defaults to `/model/event-model.json` and `/output/results.json`.

## Building

```shell
dotnet build                # Debug
dotnet test                 # run specs
dotnet build -c Release     # Release — treat warnings as errors
```

Container images are built from the repository root:

```shell
docker build -f Source/Host/Dockerfile -t cratis/stage .
docker build -f Source/SpecRunner/Dockerfile -t cratis/stage-specrunner .
```
