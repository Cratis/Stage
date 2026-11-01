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
using OneOf;
using ProtectedReads.Storage.Decisions.State;

namespace ProtectedReads.Storage.Decisions.Use;

[Command]
[BlockOnValidationSeverity(ValidationResultSeverity.Error)]
public record Consume(string SourceId, string Id) : ICanProvideEventSourceId
{
    /// <inheritdoc/>
    public EventSourceId GetEventSourceId() => Id;

    public OneOf<EventForEventSourceId, ValidationResult> Handle([LookUpBy(nameof(Consume.SourceId))] Current? current)
    {
        if (current is null) return OneOf<EventForEventSourceId, ValidationResult>.FromT1(ValidationResult.Error("Current '" + SourceId + "' does not exist."));
        if (!(!object.Equals(current.Value, ""))) return OneOf<EventForEventSourceId, ValidationResult>.FromT1(ValidationResult.Error("The stored value must not be empty."));
        return OneOf<EventForEventSourceId, ValidationResult>.FromT0(new EventForEventSourceId(Id, new Taken(current.Value)));
    }
}
```

The shape follows these rules:

- **Read protection is project-wide.** The rendered `Directory.Build.props` sets `<CratisReadProtection>On</CratisReadProtection>`, which Arc turns into assembly-level protection. Generated commands do not carry a per-command `ReadProtection` attribute. This does not replace `[ProtectedDecision]` or protected-read admission.
- **Each read is a nullable read-model parameter on `Handle`**, resolved by `[LookUpBy]` on the command property that names the key. `[LookUpBy]` is always emitted, because Arc's convention (the one command property whose type equals the view's key) is ambiguous for primitive keys: the `Consume` command has two string properties.
- **Absence is an explicit guard, first.** A missing view returns the reference message, `<View> '<key>' does not exist.`, built by concatenation, as a validation failure. Arc's own refusal of a missing read model is not a validation failure and carries a different message, so the parameter must be nullable.
- **Every requirement of a protected command runs in `Handle`, in model order, after the absence guards.** They are not rendered in the command validator. Property and concept rules stay in the parameterless validator.
- **Generation and mappings follow the guards.** Read-path mappings (`value = Current.value`) read the guarded parameter. Generated parameters follow the read parameters.
- **A command without an outcome is admitted when it reads.** It returns an empty event list: nothing is committed, and Arc still validates the reads it declared.

The renderer also emits `GeneratedCommands/when_admitting_protected_reads.cs` (Debug only), a specification that asks Chronicle whether it admits the decision reads of every protected command.

Screenplay rejects advisory reads, so the renderer never emits `[Unprotected]`. A hand-authored command customization can opt out of project-level protection with `[Cratis.Arc.Chronicle.ReadModels.Unprotected]`; that opt-out is not a modeled protected decision.

## When to use it, and when not to

Use protected reads when a command's decision depends on state another writer can change, and the decision must be invalidated if that state changes before the command commits. A lookup of a value that cannot change after it is written does not need them.

Do not combine a read with:

- a read path in a `produces when` condition or destination;
- a read-path mapping to an optional or differently typed event property (the reference yields null for a missing value, which Arc cannot express faithfully); a required collection or composite maps when the event property has exactly the same type;
- a generated-value event-source destination (freshness scopes), because the interaction with Arc's explicit scopes is unproven;
- a read of a read model whose key property is not named `id`. Chronicle's decision-read guard looks for the key only in an `id` property (a string or UUID, concepts included) and refuses any other with `KeyConversion` when the command runs. Rename the key to `id`, or read a small decision-only view keyed by `id`.

A command may combine reads with an event under an append-time constraint. The failure order is a missing read first, then the requirements, then the constraint; the three-way conformance specification `when_comparing_protected_reads_with_constraints` compares the reference evaluator, the Stage executor and the rendered application.

The renderer refuses each of these with `STAGE-ESM-044`, and the message names the reason. Non-Error requirements on protected commands remain refused with `STAGE-ESM-045`. The Contracts `EventModelLoader` admits the preamble and records `EventModel.ProtectedCommandReads`. The legacy syntax renderer, the `eventmodel` Host engine, the structural specification runner and `StageCommandHandlerProvider` refuse such a model with `STAGE-ESM-044`.

## Where the result differs from the reference

Outcomes match the reference evaluator for every specification in the canonical protected-read corpus, which the three-way conformance specification compares. The behaviors below differ outside that corpus or without changing a corpus outcome:

| Area | Reference | Generated application |
|---|---|---|
| `/validate` | Reports read absence and requirement failures | Arc's validate endpoint does not run `Handle`, so it reports neither. The Host's `validateOnly` does. |
| Generation timing | Absence is checked before generation | Generated `Handle` parameters resolve before the absence guard. Generation strategies have no side effects and run unguarded, so the outcome is unchanged. |
| Failures reported | Lists every failing requirement | Reports only the first failure. The first message is identical. |
| Race result | `DecisionStateChanged` conflict | Arc returns a validation failure with reason `ConcurrencyViolation` and its own message. No reference vector exists for races ([Screenplay#833](https://github.com/Cratis/Screenplay/issues/833)). |
| Reactors | A refusal can drive an `on refused` branch | A race is not classified as a refusal, so it is an unclassified failure that retries. |
| Reactor-invoked commands | One pipeline per invoked command | Production runs them through Arc's command side-effect executor. The generated specification runs `CommandScenario.Execute` directly. |
| Atomicity of a cascade | A command whose invoked reaction command rejects is rejected as a whole and appends nothing | The rendered application commits the triggering command; the reactor runs eventually and fails later (see [Docker hosting](../docker/index.md)). Generated specifications split the cascade into a reactor scenario and a `Trigger` scenario, and split a rejection only when the triggering command cannot reject on its own. Any other rejected cascade stays refused with `STAGE-ESM-011`. |
| Keys | Any text or UUID key | Chronicle refuses an empty key, `*`, `#` and keys with leading or trailing whitespace. Arc returns a 400 for an empty key and Chronicle faults the others. The renderer adds no guard. Whether such keys should be contract errors is open in [Screenplay#843](https://github.com/Cratis/Screenplay/issues/843). |

Removing a view's source and recreating it can fold differently from materialization when the recreating event does not set every property. The canonical corpus covers only full recreation.

## Single-instance queries

A single-instance query with a raw text or UUID key takes an event source id parameter, which satisfies Arc's `ARC0015` analyzer under warnings-as-errors. A UUID key is matched as text, so callers must send the canonical lowercase form.

## Testing

Arc's `CommandScenario` switches to decision mode for a command with read protection. In that mode the generated specification seeds prior facts with `_scenario.Given.ForEventSource(...).Events(...)` rather than `EventScenario`, which is unavailable. The three-way conformance specification runs the four source forms of the canonical corpus against the reference evaluator, the Stage executor and the rendered application, and compares each outcome.

A specification whose command produces an event that triggers a reaction invoking a protected command cannot run in one `CommandScenario`, which does not settle reactions. The renderer splits it: the reactor scenario appends the triggering event, runs the reactor and executes the command it returns, and a second specification, named with a `Trigger` suffix, runs the triggering command and asserts the event it produces. A rejection expected from the invoked command has no trigger half, because the specification lists no triggering event. That split applies only when the triggering command cannot reject on its own (no validation, requirement, decision read, route, authorization, constraint-bound event or validated concept property); otherwise the specification keeps `STAGE-ESM-011`.

Two things are tested only in the rendered application, because the reference has no race vector:

- a decision without an outcome that is accepted appends nothing;
- an event that a read depends on, appended after the read, makes the command fail with a concurrency violation, while an irrelevant event does not.

Recovering these commands with Arc's Screenplay generator is pending ([Arc#3298](https://github.com/Cratis/Arc/issues/3298)).
