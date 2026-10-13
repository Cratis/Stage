---
title: Event-source routing
description: Look up how the Cratis semantic planner renders named sources, streams and portable stream identities.
---

The Cratis semantic planner admits Screenplay ESM v8 event-source routes. A generated command appends to the declared source type, stream type and stream identity, rather than Chronicle's defaults. The generated application profile pins Chronicle 19.55.2 and Arc 22.87.0; Chronicle registers the event-source definitions used by Arc's hosted command pipeline.

## Generated definitions

An `Account` source with a `Transactions` stream produces `EventSources/AccountEventSource.cs` in `{Root}.EventSources`:

```csharp
namespace Banking.EventSources;

/// <summary>
/// Defines the account event source.
/// </summary>
[global::Cratis.Chronicle.EventSources.EventSourceAttribute("Account")]
[global::Cratis.Chronicle.EventSources.EventStreamAttribute(global::Banking.EventSources.AccountEventSource.Transactions)]
public class AccountEventSource : global::Cratis.Chronicle.EventSources.IEventSource
{
    /// <summary>
    /// Identifies the transactions stream.
    /// </summary>
    public const string Transactions = "Transactions";
}
```

The source attribute uses the **stored** source name explicitly. Recoverability applies to identity names: PascalCase names that normalization leaves unchanged, are valid C# identifiers and are not keywords. For these names, definition classes append `EventSource` to the model name, even if it already ends in that suffix. Each identity-named stream has a public string constant on the source class: its identifier is the model stream name and its value is the stored stream name. Stream attributes, command attributes and generated specification routes reference that constant directly, including when the stored name differs. Streams appear in ordinal stored-name order. Stage emits neither descriptions nor concurrency dimensions.

Other names retain the existing normalized class names and stored stream-name literals. For example, source `account` becomes `AccountEventSource`, and stream `Account_Name` uses its stored name as a literal without declaring a misleading `AccountName` constant. Such names do not round-trip through the recoverable shape; readers fall back to treating them as code. Class-name collisions fail planning with `STAGE-ESM-012`, rather than renaming a definition. Identity-named stream constants that collide with another member or the class name also fail with `STAGE-ESM-012`.

Chronicle 19.55.2's bare source attribute removes the trailing `EventSource` from the class name when deriving its stored name. Stage retains explicit source attributes instead of relying on that default.

An application render emits every source. A narrower render emits only sources referenced by selected commands, specification fixtures or observer filters, together with their identifier, stream-id and part-type dependencies. A domain placement such as `Sales/Retail` moves definitions to `Sales/Retail/EventSources/` and `{Root}.Sales.Retail.EventSources`. The shared codec remains at the application root.

## Generated commands

A routed command carries `Cratis.Arc.Chronicle.Commands.EventSourceAttribute<TSource>` with a direct reference to the stream constant, such as `AccountEventSource.Transactions`. An unkeyed stream needs only that attribute; its handler retains the existing return shape.

For a keyed stream, the handler returns `EventForEventSourceId` wrappers with `EventStreamId` set. Literal identities are formatted during planning and emitted as escaped C# literals. Property identities are formatted inside `Handle()`. Composite identities use declaration order. Every event the command produces receives the same route, even when a production targets another event-source instance. Existing `Occurred` metadata and tags remain on each wrapper.

## Observer filters

An ESM v10 reducer or reaction's `from` clause filters the facts delivered to its generated Chronicle observer:

| Filter | Generated class attribute |
|---|---|
| Source only | `global::Cratis.Chronicle.Events.EventSourceTypeAttribute("<stored source name>")` |
| Source and stream | `global::Cratis.Chronicle.EventSources.FromEventSourceAttribute<global::Banking.EventSources.AccountEventSource>(global::Banking.EventSources.AccountEventSource.Transactions)` |

`TSource` is the generated event-source definition. The stream filter references the model-named constant, whose value is the stored stream name, or uses a stored-name literal for a non-identity name. Filters do not select stream identities. Unrouted facts never match a filtered observer. A source-only filter accepts every stream of that source; a source-and-stream filter accepts only that pair. Reserved stored names fail planning with `STAGE-ESM-030`.

Reaction filters render and also apply in the semantic Host and specification executor. Generated reaction productions retain unrouted destination metadata; the observer filter does not route their output. The legacy syntax paths still refuse observer filters with `STAGE-ESM-035`. Reducer support is rendering-only: Stage's Host and in-memory specification executor still refuse reducers, and filtered reducers retain `STAGE-ESM-035` in those paths. Generated read-model companion specifications for filtered reducers fail with `STAGE-ESM-011`: Chronicle's `ReadModelScenario` does not preserve route metadata or apply observer filters.

## Identity codec

`GeneratedEventSources/StreamIds.cs` is emitted when handlers or routed specification fixtures need formatting:

| Input | Formatting |
|---|---|
| Text | Nonempty, well-formed UTF-16, Unicode NFC; returned unchanged |
| UUID | Lowercase, hyphenated `D` form using the invariant culture |
| Whole number | Invariant decimal text from the generated `long` value, within ±9007199254740991 inclusive (Screenplay's bound); values outside it fail |
| Composite | Replace `%` with `%25`, then `|` with `%7C` in each formatted part; join with `|` |

For example, parts `a|b%` and `%7C` produce `a%7Cb%25|%257C`. Scalar identities are not composite-escaped. Text is **not normalized**: decomposed text, empty text and lone surrogates fail rather than silently changing identity.

A handler reading a text or whole-number part returns an explicit `OneOf<event result, ValidationResult>`. On invalid text, or a whole number outside ±9007199254740991, it returns a value-free validation error naming the command property, and no event is returned. Composite parts are checked in declaration order before any event is created. UUID properties retain the plain wrapper return type.

## Precedence and admission

Authorization runs first, then command validation, then stream-id formatting in the handler. Stage does not emit `ICanProvideEventStreamId` or `[EventStreamId]`: Arc resolves those before authorization, which would change the modeled rejection order.

The stored source name `Default` and stored stream name `All` are reserved by Chronicle and cannot represent modeled routes. Unknown language/semantic version pairs fail `STAGE-ESM-016`. The legacy syntax renderer still refuses routes with `STAGE-ESM-030`; use the semantic planner instead.

## Rendered specifications

Generated command specifications run through Arc's `CommandScenario`, which discovers source definitions without a generated registration workaround. Routed givens are appended through the scenario's event log with their stored source type, stream type and formatted identity. Seeded events are excluded from the command's appended-event assertions.

An explicit `then` route compares `Context.EventSourceType`, `Context.EventStreamType` and `Context.EventStreamId` on the appended event. `unrouted` compares Chronicle's `EventSourceType.Default`, `EventStreamType.All` and `EventStreamId.Default`. An omitted route remains a wildcard. UUID and composite fixture identities use the same `StreamIds` helper as commands; whole-number and literal text fixture identities are validated during planning and emitted as canonical strings, so an out-of-range whole number fails planning.

At ESM v8, any-order expectations use assignment matching: a wildcard match can be reassigned so a more specific expectation gets its fact. Each fact is assigned at most once. Earlier semantic versions retain greedy matching; ordered expectations remain positional.

A fixture whose identity cannot be formatted portably fails planning with `STAGE-ESM-030`. Read-model and query companion specifications whose replay requires routed fixtures seed each given on its source, stream type and stream identity through `ReadModelScenario`, so routed reducers and filtered projections replay the same facts the command produced. Direct-append specifications remain outside the rendered subset (`STAGE-ESM-011`); the semantic reference executor supports them and, from ESM v6, compares `then` events only with facts following the append action. See [Build a renderer target](../guides/build-renderer-target.md) for the remaining construct admissions.
