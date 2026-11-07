---
title: Cratis Stage event-source routing
description: Cratis Stage renders named event sources and streams into Chronicle definitions, Arc command routes and observer filters, with portable stream IDs.
---

Cratis Stage event-source routing makes generated commands append to the source type, stream type and stream
identity declared in Screenplay. Use it when events must go to a named stream instead of Chronicle's defaults.
The semantic planner admits these routes from v8 of Screenplay's executable semantic model (ESM), the compiled
model Stage renders.

The generated application profile takes its exact Chronicle and Arc pins from Stage's `Directory.Packages.props`.
Chronicle registers the event-source definitions used by Arc's hosted command pipeline.

## Generated definitions

An `Account` source with a `Transactions` stream produces `EventSources/AccountEventSource.cs` in `{Root}.EventSources`:

```csharp
using Cratis.Chronicle.EventSources;

namespace Banking.EventSources;

/// <summary>
/// Defines the account event source.
/// </summary>
[EventSource("Account")]
[EventStream(AccountEventSource.Transactions)]
public class AccountEventSource : IEventSource
{
    /// <summary>
    /// Identifies the transactions stream.
    /// </summary>
    public const string Transactions = "Transactions";
}
```

The source attribute explicitly names the stored source. To recover the model names from generated definitions,
use identity names: PascalCase names that normalization leaves unchanged, are valid C# identifiers and are not
keywords. For these names, definition classes append `EventSource` to the model name, even if it already ends
in that suffix.

Each identity-named stream gets a public string constant on the source class. Its identifier is the model
stream name and its value is the stored stream name. Stream attributes, command attributes and generated
specification routes reference the constant directly, including when the stored name differs. Streams appear
in ordinal stored-name order. Stage emits neither descriptions nor concurrency dimensions.

Other names retain the existing normalized class names and stored stream-name literals. For example, source `account` becomes `AccountEventSource`, and stream `Account_Name` uses its stored name as a literal without declaring a misleading `AccountName` constant. Such names do not round-trip through the recoverable shape; readers fall back to treating them as code. Class-name collisions fail planning with `STAGE-ESM-012`, rather than renaming a definition. Identity-named stream constants that collide with another member or the class name also fail with `STAGE-ESM-012`.

Chronicle 19.59.6's bare source attribute removes the trailing `EventSource` from the class name when deriving its stored name. Stage retains explicit source attributes instead of relying on that default.

An application render emits every source. A narrower render emits only sources referenced by selected commands, specification fixtures or observer filters, together with their identifier, stream-id and part-type dependencies. A domain placement such as `Sales/Retail` moves definitions to `Sales/Retail/EventSources/` and `{Root}.Sales.Retail.EventSources`. The shared codec remains at the application root.

## Generated commands

A routed command carries `Cratis.Arc.Chronicle.Commands.EventSourceAttribute<TSource>` with a direct reference to the stream constant, such as `AccountEventSource.Transactions`. An unkeyed stream needs only that attribute; its handler retains the existing return shape.

For a keyed stream, the handler returns `EventForEventSourceId` wrappers with `EventStreamId` set. Literal
identities are formatted during planning and emitted as escaped C# literals. Property identities are formatted
inside `Handle()`. Composite identities use declaration order. Existing `Occurred` metadata and tags remain
on each wrapper.

Every event the command produces receives the same route, even when a production targets another event-source
instance, unless that production declares its own `stream Source.Stream` override. An override gives the
production's wrapper its own `EventSource`, `EventStream` and, for a keyed stream, `EventStreamId`. Arc takes
the wrapper's source and stream together and inherits neither from the command.

Arc 22.112.1 uses a wrapper's stream id when it differs from `EventStreamId.Default`. Otherwise it falls back
to the command context's stream id. Stage never sets that context value because it emits neither
`[EventStreamId]` nor `ICanProvideEventStreamId`.

Stage refuses two override shapes with `STAGE-ESM-036`: an override on an unkeyed stream under a keyed command
route, and a literal override stream id of `Default`. Stage has not established equivalence to reference
execution for these shapes; Arc cannot distinguish a literal `Default` from an unset wrapper stream id.

## Observer filters

An ESM v10 reducer or reaction's `from` clause filters the facts delivered to its generated Chronicle observer:

| Filter | Generated class attribute |
|---|---|
| Source only | `[EventSourceType("<stored source name>")]` from `Cratis.Chronicle.Events` |
| Source and stream | `[FromEventSource<AccountEventSource>(AccountEventSource.Transactions)]` from `Cratis.Chronicle.EventSources` |

`TSource` is the generated event-source definition. The stream filter references the model-named constant, whose value is the stored stream name, or uses a stored-name literal for a non-identity name. Filters do not select stream identities. Unrouted facts never match a filtered observer. A source-only filter accepts every stream of that source; a source-and-stream filter accepts only that pair. Reserved stored names fail planning with `STAGE-ESM-030`.

Reaction filters render and also apply in the semantic Host and specification executor. Generated reaction
productions retain unrouted destination metadata: filtering an observer's input does not route its output.
The legacy syntax paths refuse observer filters with `STAGE-ESM-035`.

Reducer support is rendering-only. Stage's Host and in-memory specification executor refuse reducers, and
filtered reducers retain `STAGE-ESM-035` in those paths. Generated read-model companion specifications for
filtered reducers fail with `STAGE-ESM-011` because Chronicle's `ReadModelScenario` does not preserve route
metadata or apply observer filters.

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
