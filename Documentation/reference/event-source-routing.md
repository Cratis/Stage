---
title: Event-source routing
description: Look up how the Cratis semantic planner renders named sources, streams and portable stream identities.
---

The Cratis semantic planner admits Screenplay ESM v8 event-source routes. A generated command appends to the declared source type, stream type and stream identity, rather than Chronicle's defaults. The generated application profile pins Chronicle 19.39.1 and Arc 22.65.3; Chronicle registers the event-source definitions used by Arc's hosted command pipeline.

## Generated definitions

An `Account` source with a `Transactions` stream produces `EventSources/AccountEventSource.cs` in `{Root}.EventSources`:

```csharp
namespace Banking.EventSources;

[global::Cratis.Chronicle.EventSources.EventSourceAttribute("Account")]
[global::Cratis.Chronicle.EventSources.EventStreamAttribute("Transactions")]
public class AccountEventSource : global::Cratis.Chronicle.EventSources.IEventSource;
```

The attributes use the **stored** source and stream names, including explicitly pinned names that differ from the declaration names. Streams appear in ordinal stored-name order. Stage emits neither descriptions nor concurrency dimensions. Definition class names retain an existing `EventSource` suffix; otherwise Stage adds it. Collisions fail planning with `STAGE-ESM-012`, rather than renaming a definition.

An application render emits every source. A narrower render emits only sources referenced by selected commands, together with their identifier, stream-id and part-type dependencies. A domain placement such as `Sales/Retail` moves definitions to `Sales/Retail/EventSources/` and `{Root}.Sales.Retail.EventSources`. The shared codec remains at the application root.

## Generated commands

A routed command carries `Cratis.Arc.Chronicle.Commands.EventSourceAttribute<TSource>` with the stored stream name. An unkeyed stream needs only that attribute; its handler retains the existing return shape.

For a keyed stream, the handler returns `EventForEventSourceId` wrappers with `EventStreamId` set. Literal identities are formatted during planning and emitted as escaped C# literals. Property identities are formatted inside `Handle()`. Composite identities use declaration order. Every event the command produces receives the same route, even when a production targets another event-source instance. Existing `Occurred` metadata and tags remain on each wrapper.

## Identity codec

`GeneratedEventSources/StreamIds.cs` is emitted when handlers or routed specification fixtures need formatting:

| Input | Formatting |
|---|---|
| Text | Nonempty, well-formed UTF-16, Unicode NFC; returned unchanged |
| UUID | Lowercase, hyphenated `D` form using the invariant culture |
| Whole number | Invariant decimal text from the generated `long` value |
| Composite | Replace `%` with `%25`, then `|` with `%7C` in each formatted part; join with `|` |

For example, parts `a|b%` and `%7C` produce `a%7Cb%25|%257C`. Scalar identities are not composite-escaped. Text is **not normalized**: decomposed text, empty text and lone surrogates fail rather than silently changing identity.

A handler reading a text part returns an explicit `OneOf<event result, ValidationResult>`. On invalid text it returns a value-free validation error naming the command property; no event is returned. UUID and whole-number properties retain the plain wrapper return type.

## Precedence and admission

Authorization runs first, then command validation, then stream-id formatting in the handler. Stage does not emit `ICanProvideEventStreamId` or `[EventStreamId]`: Arc resolves those before authorization, which would change the modeled rejection order.

The stored source name `Default` and stored stream name `All` are reserved by Chronicle and cannot represent modeled routes. Unknown language/semantic version pairs fail `STAGE-ESM-016`. The legacy syntax renderer still refuses routes with `STAGE-ESM-030`; use the semantic planner instead.

## Rendered specifications

Generated command specifications run through Arc's `CommandScenario`, which discovers source definitions without a generated registration workaround. Routed givens are appended through the scenario's event log with their stored source type, stream type and formatted identity. Seeded events are excluded from the command's appended-event assertions.

An explicit `then` route compares `Context.EventSourceType`, `Context.EventStreamType` and `Context.EventStreamId` on the appended event. `unrouted` compares Chronicle's `EventSourceType.Default`, `EventStreamType.All` and `EventStreamId.Default`. An omitted route remains a wildcard. UUID, integer and composite fixture identities use the same `StreamIds` helper as commands; literal text is validated during planning without normalization.

At ESM v8, any-order expectations use assignment matching: a wildcard match can be reassigned so a more specific expectation gets its fact. Each fact is assigned at most once. Earlier semantic versions retain greedy matching; ordered expectations remain positional.

A fixture whose identity cannot be formatted portably fails planning with `STAGE-ESM-030`. Read-model and query companion specifications whose replay requires routed fixtures also retain that refusal: their scenario cannot seed route metadata. Direct-append specifications remain outside the rendered subset (`STAGE-ESM-011`); the semantic reference executor supports them and, from ESM v6, compares `then` events only with facts following the append action. See [Build a renderer target](../guides/build-renderer-target.md) for the remaining construct admissions.
