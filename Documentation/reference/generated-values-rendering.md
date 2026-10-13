---
title: Generated values and command responses
description: Generation strategies, freshness guards, deterministic fixtures and command responses in rendered applications.
---

A create command can return its new identifier without accepting that identifier from the caller. Stage renders generated concepts as Arc Handle parameters and carries scalar or named responses back to the caller. ESM v15 adds strategy-specific factories and atomic freshness guards. The generated backend pins Arc **22.83.0**, which brings Fundamentals **7.24.0**.

## Model to C# mapping

This state-change slice generates an event-source identifier and returns it:

```screenplay
concept ProjectId : Uuid
module Projects
  feature Registration
    slice StateChange Register
      command RegisterProject
        projectId ProjectId generated identifier
        name String
        produces event ProjectRegistered
          name String = name
        returns projectId
      specification RegisteringWithAPin
        when RegisterProject
          for "11111111-1111-1111-1111-111111111111"
          name = "Apollo"
        then ProjectRegistered
          name = "Apollo"
        then returns "11111111-1111-1111-1111-111111111111"
```

Equivalent types in the generated backend are:

```csharp
using System;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Concepts;

public record ProjectId(Guid Value) : EventSourceId<Guid>(Value), IGeneratable<ProjectId>
{
    public static ProjectId New() => new(GenerateValue.Uuid());
}

[Command]
public record RegisterProject(string Name)
{
    public (ProjectId, EventForEventSourceId) Handle(
        [GeneratedValue("projectId")] ProjectId projectId) =>
        (projectId, new EventForEventSourceId(projectId, new ProjectRegistered(Name)));
}

[EventType]
public record ProjectRegistered(string Name);
```

The generated property is absent from the command constructor and request inputs. Its modeled name becomes the case-sensitive generated-value name. Mappings read the Handle parameter. An event targeting that identifier uses an explicit `EventForEventSourceId`; `GetEventSourceId()` never references a removed request property.

A generated concept that is not an identifier derives from `ConceptAs<T>` and also implements `IGeneratable<T>` with `New()`. The example above uses implicit UUID generation and stays below ESM v15; its handler remains unguarded.

## ESM v15 strategies

Declare a strategy on the concept, for example `generate uuidv7` under a Uuid concept or `generate bytes 16 hex case upper` under a String concept. The command property still says `generated`.

| Strategy | Backing type | Factory and default shape |
| --- | --- | --- |
| `uuid` | Uuid or String | `GenerateValue.Uuid()`; version 4, RFC variant, lowercase hyphenated text |
| `uuidv7` | Uuid or String | `GenerateValue.UuidV7()`; version 7, RFC variant |
| `ulid` | String | `GenerateValue.Ulid()`; 26 uppercase Crockford symbols |
| `typeid "user"` | String | `GenerateValue.TypeId("user")`; lowercase prefix, underscore and a UUID-v7 Crockford suffix; an empty prefix omits the underscore |
| `nanoid` | String | `GenerateValue.NanoId(size, alphabet)`; default size 21 and URL-safe alphabet; custom size 1–256 |
| `bytes n base64url` | String | `GenerateValue.Bytes(n, ValueEncoding.Base64Url)`; URL-safe text without padding |
| `bytes n hex` | String | `GenerateValue.Bytes(n, ValueEncoding.Hex)`; lowercase hexadecimal |
| `bytes n base32` | String | `GenerateValue.Bytes(n, ValueEncoding.Base32)`; uppercase base32 without padding |

String-backed strategies accept the modifiers admitted by Screenplay: `prefix` adds printable ASCII text before the generated portion, and `case upper` or `case lower` selects its casing without changing the prefix. Bytes accept counts 1–512; unused trailing encoding bits must be zero. Screenplay refuses unsupported strategies and identifiers below 64 random bits before rendering.

### Fresh event sources

At v15, a command that produces facts on a generated destination returns `EventsWithConcurrencyScopes`, with one `ConcurrencyScope(EventSequenceNumber.BeforeFirst, EventSourceId: id)` per generated source. Chronicle checks the source is empty atomically when appending; several initial facts in the same invocation are allowed. A previously used source rejects the entire command with `ValidationResultReason.ConcurrencyViolation`. Generated rejection specifications assert that reason rather than Arc's message text.

The Host and specification executor use the reference freshness message: "A generated event source already has facts; retry the command with a fresh generated value." Earlier facts without a destination never make the generated source stale. Below v15, handlers remain unguarded. These explicit scopes will be replaced when [Arc #3129](https://github.com/Cratis/Arc/issues/3129) supplies its generated-identifier freshness contract.

## Conditional productions and shared slices

A StateChange slice can declare several commands. Each command keeps its own validator, response and specifications, while sharing the slice's event contracts and source file.

Portable `produces when` conditions read command inputs and generated values. Stage evaluates each condition once and returns a typed collection of `EventForEventSourceId` values containing only the taken productions. At v15, freshness scopes cover only sources targeted by those productions. If every production is skipped, the command is accepted, appends nothing and still returns its response, even if the generated source already has facts.

Conditions support text and Boolean equality, numeric equality and ordering, and `and`/`or`. Enum operands and expression-valued `Condition` nodes remain refused with `STAGE-ESM-006`; production route overrides remain refused with `STAGE-ESM-036`. A specification that reads a generated property in a production condition must pin it, so the expected production sequence can be determined before rendering.

Commands without production conditions retain their existing generated Handle shape.

## Responses

| Screenplay contract | C# Handle response |
| --- | --- |
| `returns projectId` | The property's type, paired with the event result |
| `returns` block | A `<Command>Response` record, paired with the event result |
| Response-only command | The scalar or response record directly, without an event tuple |

A response block names each field independently of its source property. Each field keeps the source type, including optionality. Generated parameters and ordinary command properties can supply response values. Arc uses the tuple's response element as `CommandResult<T>.Response`; it does not expose the event wrapper as the response.

The generated TypeScript proxy carries the response type too. A UUID-backed scalar response uses `Command<IRegisterProject, Guid>`; a named response uses `Command<IRegisterReceipt, RegisterReceiptResponse>`. The event wrapper is not the client response type.

## Fixtures and testing

A specification's `when ... for <uuid>` pins a generated identifier. `generated receipt = "<uuid>"` pins a generated non-identifier property. Stage emits `PinGeneratedValue` before executing the scenario, using the concept type rather than a raw `Guid`.

For the model above, the generated setup contains this fragment:

```csharp
_scenario.PinGeneratedValue("projectId", new ProjectId(Guid.Parse("11111111-1111-1111-1111-111111111111")));
```

Pinned event and `then returns` assertions compare the authored values. A named return expectation can assert a subset of the response fields.

When a rendered specification omits a fixture, Stage calls `_scenario.AllowGeneration()`. Ordered event destinations and payload mappings that depend on that unpinned property compare against `_scenario.GeneratedValues.Last<T>(name)`, and response assertions compare against the same capture. Below v15, the UUID must be nonempty. At v15, every unpinned capture must also match its strategy's derived encoding shape. These are relational assertions, not assertions that random generation equals an authored UUID literal. Unordered or projected expectations require pins.

The in-memory specification executor requires deterministic fixtures, just like the Screenplay evaluator. Missing fixtures produce an `Unsupported(IdentityAllocation)` outcome; they do not silently allocate a value and pass. It compares scalar responses and each asserted named field after successful execution.

Rendering is not compilation or execution. Build the generated project in Debug, then run its Debug specifications. A successful run proves that Arc resolves the parameters, the events land on the intended streams, and the response assertions execute.

## Partial-perform runtime

The live semantic runtime supplies a value from the concept's strategy as a semantic text value for every generated property, including validation-only requests. Models below v15 retain UUID generation. It rejects attempts to supply generated properties as request inputs. Validation-only requests do not append facts or reserve the generated UUID for a later execution.

An accepted scalar response becomes the HTTP command result's `response` value. A named response becomes an object with camelCase field names. Rejected or unauthorized results carry no response. The Stage frontend passes the accepted response through Scene's `CommandOutcome.response`.

Form `on submit` response binding and navigation remain blocked on [Scene #53](https://github.com/Cratis/Scene/issues/53). Generated TypeScript proxies already expose the scalar or named command response; they do not supply Scene's continuation binding.

## Limits and diagnostics

| Diagnostic | Refused shape |
| --- | --- |
| `STAGE-ESM-028` | Unknown generation strategies, non-concept generated values, optional values and collections; the legacy syntax engine refuses every explicit strategy |
| `STAGE-ESM-011` | A v15 generated fixture that does not match its strategy shape |
| `STAGE-ESM-004` | StateChange slices without a command |
| `STAGE-ESM-006` | Expression conditions, enum operands and nonnumeric ordering in production conditions |
| `STAGE-ESM-038` | Generated values or responses combined with command routing; generated-property rules or requirements, generated-concept validation rules, occurrence or conditional Handle local-name collisions; unsupported response sources, composite or collection responses; response/factory name collisions; generated values mapped into unique-property constraints |
| `STAGE-ESM-038` | Unpinned generation read by a production condition, or with unordered event, read-model or query expectations |
| `STAGE-ESM-038` | A generated identifier combined with a destination-less plain production |

Generated-concept validation is refused because Arc treats an invalid generated value as a server exception, whereas the evaluator treats it as a rejection. Stage does not translate one outcome into the other.

The last case is not shorthand for using the generated identifier. Screenplay keeps legacy allocation in a separate `AllocatedIdentities` channel. Use an inline `produces event` declaration as above, or an explicit production destination. Stage refuses the ambiguous combination instead of sending the event to a different stream than the evaluator.

Time-ordered factories (`uuidv7`, `ulid`, `typeid`) use the wall clock. Arc calls `IGeneratable<T>.New()` rather than a receipt-time `IGeneratableInContext<T>` factory, so Stage does not claim command-receipt timestamps for these values. Pinned fixtures are unaffected. Sequences and clock fixtures remain unsupported.

## Pitfalls

- Do not put a generated identifier back into the request to make a spec deterministic. Pin it in the scenario instead.
- Do not edit generated commands or concepts to select a strategy. Declare it on the concept in the model.
- Moving an implicit UUID fixture to v15 requires a lowercase version-4 UUID with the RFC variant. A pin such as `11111111-1111-1111-1111-111111111111` no longer matches; uppercase Uuid text is noncanonical even before the strategy check.
- A pin has the exact concept type and modeled name. A raw UUID or differently cased name is not the same fixture.
- A returned UUID is not proof that a projection is ready, an external effect succeeded, or an identifier is absent from storage.

For the complete target admission table, see [Build a renderer target](../guides/build-renderer-target.md).
