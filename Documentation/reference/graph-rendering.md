---
title: Graph rendering
description: Stage renders Screenplay graph declarations and projections as Chronicle node and edge records, connects the host to Neo4j, and refuses unsupported constructs.
---

A Screenplay `graph` declares a property graph, and a `projection ... => graph` builds it from events. Stage renders both for the ESM v38 pair as the model-bound graph types of Chronicle's graph projection API, and connects the generated host to Neo4j. This page describes what is rendered, what is refused and why, and the one piece that waits for a Chronicle release.

> [!NOTE]
> Graph rendering needs a Chronicle client that carries the graph API (`[Node]`, `[Edge]`, `GraphScenario`), which ships from Chronicle 19.129.0. Stage pins that release, so a model that declares a graph renders. A Stage pinned to an older Chronicle refuses it with `STAGE-ESM-063`.

## What is rendered

For the [Organization sample](https://github.com/Cratis/Screenplay/tree/main/Samples/Organization), an application plan adds these files under `Graphs/`:

| Modeled construct | Generated |
| --- | --- |
| `node Person` | `Graphs/Organization/Person.cs`: a `record` named after the label, marked `[Graph("Organization")]`, `[Node]` and `[NoAutoMap]` |
| The `key` property of a node | The `[Key]` parameter of the record. When the key is a concept that is an event source identity, the parameter is the value it holds (for example `Guid`), because Chronicle's analyzer refuses `[Key]` on an event source identity while the graph reader requires it |
| `from E key <value>` on a node | `[FromEvent<E>]`, with the event property name when the key is an event property and `ConstantKey` when it is a literal |
| `remove with E key <value> [detach]` | `[RemovedWith<E>]`, with `Detach = true` for `detach` |
| `edge MEMBER_OF source Person target Group` | `Graphs/Organization/MemberOf.cs`: a record marked `[Edge("MEMBER_OF")]` whose `SourceKey` is `[Source<Person>]` and `TargetKey` is `[Target<Group>]` |
| `from E source <value> target <value>` on an edge | `[EdgeFrom<E>(sourceKey: ..., targetKey: ...)]` |
| `remove with E source ... target ...` on an edge | `[EdgeRemovedWith<E>(sourceKey: ..., targetKey: ...)]` |
| `identified by assignmentId` | The identifying property is the edge's `[Key]`, and every handler passes `identity:` |
| `set`, `add`, `subtract`, `increment`, `decrement` mappings | `[SetFrom<E>]`, `[SetFromContext<E>]`, `[SetValue<E>]`, `[AddFrom<E>]`, `[SubtractFrom<E>]`, `[Increment<E>]` and `[Decrement<E>]` on the property |
| `then graph` | `Graphs/Specifications/when_<name>_is_graphed.cs`: a specification that appends the events to a `GraphScenario`, reads each node and edge back and asserts it |

The attributes are Chronicle's own, so the engine behaves as it does for a hand-written graph. A node's key and an edge's identity are said once by the handler and are not mapped a second time. The fluent `IGraphProjection` is never needed: every construct is either expressible with attributes or refused.

A `StateView` slice that only hosts a graph has no read model, so it renders nothing of its own; the graph renders with the application.

### Host and configuration

When the model declares a graph, the application scaffold also connects to Neo4j:

- `Program.cs` calls `WithNeo4j()` on the Arc builder, so a query can inject Arc's tenant-aware `IGraphSession`.
- The project references `Cratis.Arc.Neo4j`, pinned centrally at the Arc release Stage pins.
- `appsettings.json` gets a `Cratis:Neo4j` section: the server, the event store (the application name, which is the event store Chronicle's Neo4j sink writes), the user name and `PasswordSecret: NEO4J_PASSWORD`. The password itself is never written.
- `docker-compose.yml` gets a development Neo4j server and configures Chronicle's Neo4j sink. Both read the password from the `NEO4J_PASSWORD` environment variable of the person running Compose, and Compose refuses to start without it.
- The README gets a *Graphs* section.

The development setup uses `DatabaseMode: Single` because Neo4j Community Edition has one user database. That is for development only; production uses the per-namespace default against a server that supports it.

## What is refused

A graph construct that cannot be expressed is refused with a named diagnostic. It is never dropped.

| Code | Meaning |
| --- | --- |
| `STAGE-ESM-063` | The model declares a graph, graph projection or graph specification, and the pinned Chronicle client does not carry the graph API yet. The whole graph is refused. |
| `STAGE-ESM-062` | A graph construct Chronicle's model-bound API cannot express: a node label that is not a PascalCase identifier (the label is the record's name), two elements that render to one record, an edge identified by a property it does not declare, a mapping with a factor (`add x by a * b`), a `clear` mapping, a literal that is not a text, boolean or whole-number constant, a key read from something other than an event property, the event source or a literal, an event handled twice by one element, or an element built by two projections. |
| `STAGE-ESM-064` | A graph specification the generated `GraphScenario` cannot express. A specification that seeds a graph with `given graph` is omitted with this code as a degradation warning (`STAGE-DEGRADE-002`), because the scenario builds a graph only from appended events; a `Strict` plan fails acceptance on it. Other malformed graph specifications are errors. |
| `STAGE-ESM-065` | The live runtime (Play) registers each graph and its projection with the kernel it runs against, and the kernel's graph sink projects them (Neo4j by default, `Stage__Graph__SinkType=InMemoryGraph` for a kernel with no Neo4j). It does not read a graph back, and reports that as a non-blocking note on admission; graph specifications are decided by Screenplay's reference graph runner. |
| `STAGE-ESM-066` | A scoped plan leaves out the graphs of the application. Graphs render with the application scope. |
| `STAGE-ESM-068` | The model declares a query that reads a graph (`query ... graph <Graph>`, ESM v40). The sandbox runs it against Neo4j; the renderer does not render a graph query yet, so rendering refuses the model rather than leave the query without an endpoint. |
| `STAGE-ESM-067` | The legacy syntax path (the structural engines that translate a parsed document rather than the executable semantic model) met a graph, graph projection or graph specification step. It would drop it, so it refuses it. |
| `STAGE-ESM-016` | ESM v38 models can also carry the v37 clock and time-zone constructs and v37.1 property order, which Stage has not audited. They are refused like the members of an unaudited pair. |

## Verifying graph specifications

The specification executor runs `given graph` and `then graph` specifications with Screenplay's reference graph runner, which applies the five graph operations (upsert node, set properties, delete node, upsert edge, delete edge) to an in-memory property graph. The results are reported with the slice kind `Graph` and are selected with the slice that owns them, or with any module, feature or application scope that contains it.

## The Chronicle release gate

`ChronicleGraphSupport.FirstRelease` in `Source/Rendering.Cratis/Semantics/Graphs/ChronicleGraphSupport.cs` is the first Chronicle release that carries the graph API, 19.129.0. The renderer admits a graph only when the Chronicle version Stage pins is at or past it; otherwise it refuses the whole graph with `STAGE-ESM-063`. Raising the Chronicle pins in `Directory.Packages.props` needs no change here, and the framework name catalog is regenerated with `STAGE_WRITE_FRAMEWORK_NAME_CATALOG=1 dotnet test Source/Rendering.Cratis/Rendering.Cratis.csproj --filter "FullyQualifiedName~for_FrameworkNameCatalog"`.
