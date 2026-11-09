---
title: Named sinks
description: How Stage moves Screenplay sink declarations into Chronicle when running or rendering a model, what the manifest lists, and why named-sink queries are refused.
---

A Screenplay model can name the sinks its read models and graphs are stored in. `sink Reporting for readmodels` and `sink People for graphs` declare a sink at the application root, and a `sink <Name>` directive in a read model or a graph chooses one. A read model or graph that names no sink uses the default sink of its kind, so a model without sinks behaves exactly as it always has. The model never names an engine: whoever runs the application maps each name to storage, through Chronicle's [named sinks configuration](/chronicle/hosting/configuration/sinks/).

```text
sink Reporting for readmodels
sink People for graphs

module Organization
  feature Chart
    slice StateView Directory
      readmodel Listing
        sink Reporting
        id   PersonId key
        name PersonName
```

## How sinks are carried

Sinks are realization metadata, like `passive`: they sit outside the portable executable model, so adding, changing or removing a `sink` directive leaves the model's ESM bytes and revision unchanged. Stage carries them in `ApplicationRealization.Sinks` (`RealizedSinks`): the declared sinks with their kinds, the sink of each read model that names one, and the sink of each graph that names one.

For rendering, the sinks travel in their own hashed profile input, `cratis-realization:sinks`, so changing a `sink` directive changes the render input hash even though the revision stays the same. A model without sinks adds no input, and its render input hash is the one it always had. Source-backed planning adds the input itself; a programmatic caller uses `CratisRendering.WithRealization(profile, compilation)` or `CratisRendering.WithSinks(profile, compilation)` from the same compilation. A malformed, duplicate, noncanonical or inconsistent input fails profile admission: one that names a read model or graph the model does not declare, a sink the model does not declare, or a sink of the wrong kind.

## Running a model

The semantic engine answers commands and queries over read models in process, so a read model's sink does not change how Stage answers it, and Stage registers no read model with a sink.

Graphs are projected only by the kernel's graph sink. `SemanticGraphMirrors` registers each graph's read model with the kernel and sets the sink name from the model, so a graph that names `People` is registered with `SinkDefinition.Name = "People"`. `Stage__Graph__SinkType` still chooses the sink type, Neo4j by default. The kernel Stage runs against must configure every named graph sink, or it refuses the registration; see [Graph rendering](graph-rendering.md) for the graph sink itself.

## The published package

The package's `manifest.json` lists the declared sinks with their kinds, in declaration order, and an empty list when the model declares none:

```json
"sinks": [
  { "name": "Reporting", "kind": "readmodels" },
  { "name": "People", "kind": "graphs" }
]
```

A runtime that deploys the package uses the list to check that the deployment maps every name before the kernel refuses a registration. The property is additive: the schema version stays `1`, and a manifest written before sinks existed reads as having none. See [Published applications](published-applications.md#manifestjson).

## Rendering

A read model or graph that names a sink renders Chronicle's `[Sink("<Name>")]` attribute: on the read model's record, and on every node and edge record of the graph, since Chronicle requires all elements of one graph to name the same sink. Screenplay refuses a sink on a passive read model (`PLAY0935`), as Chronicle's `CHR0069` refuses `[Sink]` with `[Passive]`.

The rendered `docker-compose.yml` configures the development kernel with one configuration per declared sink, so the rendered application runs without further setup:

| Sink kind | Kernel configuration |
| --- | --- |
| `readmodels` | `Type: MongoDB` on the development image's own MongoDB, `mongodb://localhost:27017` |
| `graphs` | `Type: Neo4j` on the development Neo4j server, `bolt://neo4j:7687`, user `neo4j`, `DatabaseMode: Single`, with the password read from `NEO4J_PASSWORD` as the graph scaffold does |

The variables are `Cratis__Chronicle__Sinks__Configurations__<Name>__*` on the `chronicle` service. The kernel's default sinks are left as they are. A deployment replaces these with its own storage for each name. The application's `appsettings.json` is unchanged: it configures the application's own Arc and Chronicle client, not the kernel, and the client finds a read model's or graph's sink from its `[Sink]` attribute.

### A query over a read model in a named sink

Rendered queries read their read model through Arc's MongoDB support (`WithMongoDB`), from the application's own MongoDB database. The pinned Arc release cannot read a read model from a named sink, which may be another database or another server, so a rendered query would silently read the wrong database. Stage refuses such a model instead:

| Code | Meaning |
| --- | --- |
| `STAGE-ESM-069` | A query reads a read model that names a sink. The diagnostic names the query, the read model and the sink. Nothing is rendered. Keep the read model in the default sink, or render once Arc can read a named sink. |

A read model in a named sink that no query reads renders: Chronicle writes it to its sink, and commands that read it go through Chronicle. Graph queries are not rendered at all yet (`STAGE-ESM-068`), so a graph in a named sink raises no read-side refusal.
