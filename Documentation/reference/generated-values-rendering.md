---
title: Generated values and command responses
description: How Stage renders server-owned UUID concepts, deterministic generation fixtures and command responses.
---

A create command can return its new identifier without accepting that identifier from the caller. Stage renders generated UUID concepts as Arc Handle parameters and carries scalar or named responses back to the caller. The generated backend pins Arc **22.78.0**.

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

A generated UUID concept that is not an identifier derives from `ConceptAs<Guid>` and also implements `IGeneratable<T>` with `New()`. Existing UUID identifier factories use `GenerateValue.Uuid()` too.

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

When a rendered specification omits a fixture, Stage calls `_scenario.AllowGeneration()`. Ordered event destinations and payload mappings that depend on that unpinned property compare against `_scenario.GeneratedValues.Last<T>(name)`, and response assertions compare against the same capture. The UUID must be nonempty. These are relational assertions, not assertions that random generation equals an authored UUID literal. Unordered or projected expectations require pins.

The in-memory specification executor requires deterministic fixtures, just like the Screenplay evaluator. Missing fixtures produce an `Unsupported(IdentityAllocation)` outcome; they do not silently allocate a value and pass. It compares scalar responses and each asserted named field after successful execution.

Rendering is not compilation or execution. Build the generated project in Debug, then run its Debug specifications. A successful run proves that Arc resolves the parameters, the events land on the intended streams, and the response assertions execute.

## Partial-perform runtime

The live semantic runtime supplies a fresh UUID as a semantic text value for every generated property, including validation-only requests. It rejects attempts to supply generated properties as request inputs. Validation-only requests do not append facts or reserve the generated UUID for a later execution.

An accepted scalar response becomes the HTTP command result's `response` value. A named response becomes an object with camelCase field names. Rejected or unauthorized results carry no response. The Stage frontend passes the accepted response through Scene's `CommandOutcome.response`.

Form `on submit` response binding and navigation remain blocked on [Scene #53](https://github.com/Cratis/Scene/issues/53). Generated TypeScript proxies already expose the scalar or named command response; they do not supply Scene's continuation binding.

## Limits and diagnostics

| Diagnostic | Refused shape |
| --- | --- |
| `STAGE-ESM-028` | Generated values other than required, scalar UUID-backed concepts: raw UUIDs, optional values, collections and other backing types |
| `STAGE-ESM-038` | Generated values or responses combined with command routing; generated-property rules or requirements, generated-concept validation rules, occurrence parameter name collisions; unsupported response sources, composite or collection responses; response/factory name collisions; generated values mapped into unique-property constraints |
| `STAGE-ESM-038` | Unpinned generation with unordered event, read-model or query expectations |
| `STAGE-ESM-038` | A generated identifier combined with a destination-less plain production |

Generated-concept validation is refused because Arc treats an invalid generated value as a server exception, whereas the evaluator treats it as a rejection. Stage does not translate one outcome into the other.

The last case is not shorthand for using the generated identifier. Screenplay keeps legacy allocation in a separate `AllocatedIdentities` channel. Use an inline `produces event` declaration as above, or an explicit production destination. Stage refuses the ambiguous combination instead of sending the event to a different stream than the evaluator.

Stage's phase-1 mapping always uses `GenerateValue.Uuid()`. Screenplay's ESM carries no generation strategy or freshness policy, so Stage does not render UuidV7, guarded freshness, `IGeneratableInContext<T>`, sequences or clock-dependent generation. Clock fixtures remain unsupported automation constructs. These limits concern Stage's mapping; they do not redefine the capabilities of Arc's generation helpers.

## Pitfalls

- Do not put a generated identifier back into the request to make a spec deterministic. Pin it in the scenario instead.
- Do not edit generated commands or concepts to select a strategy. Keep the model authoritative and wait for an admitted strategy contract.
- A pin has the exact concept type and modeled name. A raw UUID or differently cased name is not the same fixture.
- A returned UUID is not proof that a projection is ready, an external effect succeeded, or an identifier is absent from storage.

For the complete target admission table, see [Build a renderer target](../guides/build-renderer-target.md).
