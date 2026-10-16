---
title: Protected reads rendering
description: Look up how the Cratis renderer turns Screenplay protected command reads into Arc read protection, where the result differs from the reference, and how to test it.
---

A command that decides against a view states it with `reads`. In a model that declares `command reads protected` (ESM v25), the read guards the decision: the command may commit only while the facts it read are still the facts the store holds. The Cratis renderer turns each such command into an Arc command with read protection, so Chronicle checks that guard when the command commits.

## What the renderer emits

For this model, the command reads the `Current` view by `sourceId`, requires its value to be non-empty and copies the value into an event:

```screenplay
command Consume
  id String identifier
  sourceId String
  reads Current by sourceId
  validate
    require Current.value != ""
      message "The stored value must not be empty."
  produces Taken
    for id
    value = Current.value
```

the renderer emits:

```csharp
[global::Cratis.Arc.Commands.ModelBound.CommandAttribute]
[global::Cratis.Arc.Commands.ReadProtectionAttribute(global::Cratis.Arc.Commands.ReadProtectionMode.On)]
[global::Cratis.Arc.Commands.ModelBound.BlockOnValidationSeverityAttribute(global::Cratis.Arc.Validation.ValidationResultSeverity.Error)]
public record Consume(string SourceId, string Id) : global::Cratis.Chronicle.Events.ICanProvideEventSourceId
{
    public global::OneOf.OneOf<global::Cratis.Chronicle.EventSequences.EventForEventSourceId, global::Cratis.Arc.Validation.ValidationResult> Handle(
        [global::Cratis.Arc.Commands.LookUpByAttribute(nameof(Consume.SourceId))] global::ProtectedReads.Storage.Decisions.State.Current? current)
    {
        if (current is null) return /* ValidationResult.Error("Current '" + SourceId + "' does not exist.") */;
        if (!(!global::System.Object.Equals(current.Value, ""))) return /* ValidationResult.Error("The stored value must not be empty.") */;
        return /* the Taken event, with Value = current.Value */;
    }
}
```

The shape follows these rules:

- **Read protection is per command.** `ReadProtectionMode.On` is declared only on a command that has `reads`. Commands without reads keep the ordinary scenario and are not affected.
- **Each read is a nullable read-model parameter on `Handle`**, resolved by `[LookUpBy]` on the command property that names the key. `[LookUpBy]` is always emitted, because Arc's convention (the one command property whose type equals the view's key) is ambiguous for primitive keys: the `Consume` command has two string properties.
- **Absence is an explicit guard, first.** A missing view returns the reference message, `<View> '<key>' does not exist.`, built by concatenation, as a validation failure. Arc's own refusal of a missing read model is not a validation failure and carries a different message, so the parameter must be nullable.
- **Every requirement of a protected command runs in `Handle`, in model order, after the absence guards.** They are not rendered in the command validator. Property and concept rules stay in the parameterless validator.
- **Generation and mappings follow the guards.** Read-path mappings (`value = Current.value`) read the guarded parameter. Generated parameters follow the read parameters.
- **A command without an outcome is admitted when it reads.** It returns an empty event list: nothing is committed, and Arc still validates the reads it declared.

The renderer also emits `GeneratedCommands/when_admitting_protected_reads.cs` (Debug only), a specification that asks Chronicle whether it admits the decision reads of every protected command.

## When to use it, and when not to

Use protected reads when a command's decision depends on state another writer can change, and the decision must be invalidated if that state changes before the command commits. A lookup of a value that cannot change after it is written does not need them.

Do not combine a read with:

- a read path in a `produces when` condition or destination;
- a read-path mapping to an optional, collection, composite or differently typed event property (the reference yields null for a missing value, which Arc cannot express faithfully);
- a generated-value event-source destination (freshness scopes), because the interaction with Arc's explicit scopes is unproven;
- an event that is a target of an append-time constraint.

The renderer refuses each of these with `STAGE-ESM-044`, and the message names the reason. Non-Error requirements on protected commands remain refused with `STAGE-ESM-045`. The legacy syntax renderer, Host and structural specification runner refuse the preamble with `STAGE-ESM-044`.

## Where the result differs from the reference

Outcomes match the reference evaluator for every specification in the canonical corpus. These behaviors differ, and none changes a canonical outcome:

| Area | Reference | Generated application |
|---|---|---|
| `/validate` | Reports read absence and requirement failures | Arc's validate endpoint does not run `Handle`, so it reports neither. The Host's `validateOnly` does. |
| Generation timing | Absence is checked before generation | Generated `Handle` parameters resolve before the absence guard. Generation strategies have no side effects and run unguarded, so the outcome is unchanged. |
| Failures reported | Lists every failing requirement | Reports only the first failure. The first message is identical. |
| Race result | `DecisionStateChanged` conflict | Arc returns a validation failure with reason `ConcurrencyViolation` and its own message. No reference vector exists for races ([Screenplay#833](https://github.com/Cratis/Screenplay/issues/833)). |
| Reactors | A refusal can drive an `on refused` branch | A race is not classified as a refusal, so it is an unclassified failure that retries. |
| Reactor-invoked commands | One pipeline per invoked command | Production runs them through Arc's command side-effect executor. The generated specification runs `CommandScenario.Execute` directly. |
| Keys | Any text or UUID key | Chronicle refuses an empty key, `*`, `#` and keys with leading or trailing whitespace. Arc returns a 400 for an empty key and Chronicle faults the others. The renderer adds no guard. Whether such keys should be contract errors is open in [Screenplay#843](https://github.com/Cratis/Screenplay/issues/843). |

Removing a view's source and recreating it can fold differently from materialization when the recreating event does not set every property. The canonical corpus covers only full recreation.

## Testing

Arc's `CommandScenario` switches to decision mode for a command with read protection. In that mode the generated specification seeds prior facts with `_scenario.Given.ForEventSource(...).Events(...)` rather than `EventScenario`, which is unavailable. The three-way conformance specification runs the four source forms of the canonical corpus against the reference evaluator, the Stage executor and the rendered application, and compares each outcome.

Two things are tested only in the rendered application, because the reference has no race vector:

- a decision without an outcome that is accepted appends nothing;
- an event that a read depends on, appended after the read, makes the command fail with a concurrency violation, while an irrelevant event does not.

Recovering these commands with Arc's Screenplay generator is pending ([Arc#3298](https://github.com/Cratis/Arc/issues/3298)).
