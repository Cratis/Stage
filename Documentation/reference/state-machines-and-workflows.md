---
title: Cratis Stage state machine and workflow execution
description: Cratis Stage drives supported state machines and workflows from committed facts. Compare sandbox execution, specification checks, and generated code.
---

Cratis Stage drives supported Screenplay state machines and workflows from committed facts, so you can follow a subject's
state and inspect the process it starts. Use this page to distinguish what a sandbox session executes, what specifications
check, and what the renderer can generate. Passing a specification does not establish that a construct can run in the sandbox
or be rendered.

A Screenplay `statemachine` describes where one thing is in its lifecycle and what entering a state does. A `workflow`
describes a process that invokes commands, chooses paths and waits for an event, a deadline or a person's decision. Both are
top-level declarations admitted from executable semantic model (ESM) v36. The sandbox uses the reference evaluator to derive
state-machine states and the Cratis Choreography workflow engine to run workflows. That engine runs on Stage's hosted
Orleans silo, the server that hosts its grains (addressable runtime objects). See [ports and clustering](../docker/index.md)
for the hosting configuration.

## What the runtime does

When Stage performs a model, its semantic runtime keeps the committed facts and derives read models from them. Each state
machine instance follows the facts for its subject key. Workflows use that same key to identify the subject they act on.

| Construct | Runtime behavior |
| --- | --- |
| State machine | Derived from committed facts with the same rule the specifications use: take the first transition on the event, in declaration order, whose guard (condition) holds over the followed read model after that event and whose target the current state allows. Otherwise, leave the state unchanged. |
| `entering invokes <Command>` | The runtime invokes the command through the semantic runtime, supplying the subject key as the command's identifier. This path does not fill other properties from the followed read model. |
| `entering starts <Workflow>` | The runtime admits a run of a supported workflow on the silo. |
| Workflow | Each workflow becomes a definition snapshot served by an `IWorkflowDefinitions` source built from the loaded model. Actions invoke commands, decisions evaluate conditions over the current subject read model, and the engine handles timers and expiries. |
| Event trigger, state trigger | A fact the trigger names, or a machine entering the state it names, admits a run with a deduplication key identifying that subject and triggering occurrence. |
| `wait ... for <Event>` | The fact resumes the runs waiting at the node for the same subject. |
| `approval` | The run is suspended until a person decides it. |

A workflow action that invokes a command leaves through `next` when the command is accepted. It reports a command that was handed on and
rejected, by validation, authorization or a constraint, through `refused`, and a command that could not run, or did not settle, through
`failed`. The engine retries a `failed` action up to its `attempts`. When the model connects `refused`, a refusal leaves through that outlet;
when it does not, the refusal fails the node. Abilities of a `system` run the same way, as durable invocations: see
[Durable ability invocations](ability-invocations.md).

A state's declared transitions determine which target states it allows. For a new event, the evaluator searches transitions
across the whole machine, not only those written under the current state.

A workflow run starts with the current values of the subject read model. When a workflow fans out, it processes list items as
separate branches. The engine expects each item to have a unique string `key`: the runtime keeps an existing key or supplies
the item's list position, and wraps a scalar item in an object holding its `value`. An action's command takes the subject key
as its identifier and other properties from the fan-out item first, then from the subject snapshot.

The lifecycle driver starts listening after the world has been reconstructed and acts only on facts committed after it
starts. It does not replay historical facts into side effects. This is not an exactly-once guarantee for commands.

Stage registers `AddEventSourcedWorkflows`, so `ChronicleWorkflowRunJournal` appends run facts to Cratis Chronicle and reloads
them when a run grain activates. A host restart does not by itself erase that history if Chronicle's data is retained.
Stage's default in-memory grain storage makes the background sweep's active-run index volatile. Stage also keeps the run IDs
used for lifecycle inspection, event resumption and approval decisions only in memory; it does not rebuild that tracking from
the journal. Do not use the sandbox to retain a business process across sessions. Deriving a state-machine state from facts
is separate from restoring workflow execution.

### Where a state machine is

```bash
curl http://localhost:8080/stage/lifecycles
```

The response reports each state machine's instances and their derived states. For each workflow it lists the runs the
sandbox started, their status and the nodes where they are waiting. Anything the runtime refuses is listed under `refused`.
The examples use a host listening on port `8080`; use your session's published address when calling these paths.

### Deciding an approval

An approval node suspends its run. Decide it for one subject:

```bash
curl -X POST http://localhost:8080/stage/lifecycles/workflows/Collect/subjects/9c858901-8a57-4791-81fe-4c455b099bc9/approvals/Approve \
  -H 'Content-Type: application/json' \
  -d '{"approved": true}'
```

The response is `202` when a run was waiting at the approval and `404` when none was. The sandbox does not check the approver
persona or policy the model names: anyone who can reach the endpoint decides. Treat it as a development surface.

### What the runtime refuses

A construct the runtime cannot drive is named on `GET /stage/semantic/admission` and in the log with a `STAGE-LIFECYCLE-*` code.
A lifecycle refusal does not block the rest of the model. Its specifications remain eligible for reference evaluation;
that evaluation does not make the refused construct executable.

| Code | Refused because |
| --- | --- |
| `STAGE-LIFECYCLE-001` | A workflow does not have exactly one trigger. Write one workflow per trigger. |
| `STAGE-LIFECYCLE-003` | A foreach sits inside the branches of another foreach. |
| `STAGE-LIFECYCLE-004` | A foreach is not gathered by exactly one join. |
| `STAGE-LIFECYCLE-005` | A join does not gather the branches of a foreach. |
| `STAGE-LIFECYCLE-006` | An edge leaves by an outlet the engine node does not have. |
| `STAGE-LIFECYCLE-007` | A trigger is a schedule. |

## Specifications

The spec runner checks specifications inside state machines and workflows through the reference lifecycle evaluator, not by
starting workflow runs on the silo. Select them with the application scope, or with no scope at all; a module, feature or slice
scope leaves them out. Use these checks to verify modeled transitions and process outcomes, then inspect runtime admission
before trying the same behavior in a session.

## What the renderer emits

Rendering produces application code rather than exporting the sandbox's active runs. The generated application uses
Cratis Choreography through the `Cratis.Choreography` APIs pinned by Stage. It gets the following in its `Lifecycles` namespace:

| Model | Generated code |
| --- | --- |
| `statemachine` | An `EventSourcedStateMachine<TModel>` grain with a nested `State<TModel>` per state, `AllowedTransitions`, the transitions `On<TEvent>().When(...).TransitionTo<TState>()` in declaration order, and an `OnEnter` that sends the command through `ICommandPipeline` or starts the workflow. |
| `workflow` | A definition class implementing `IWorkflowDefinitions`, performers for action commands, reactors for event waits, and `GeneratedWorkflows.Configure` when there are event triggers. |

The generated `Program.cs` passes `GeneratedWorkflows.Configure` to `AddEventSourcedWorkflows` when the model has an event trigger.
The same registration journals workflow run facts in Chronicle, not in grain storage. The generated silo uses in-memory grain
storage by default, so its background sweep's active-run index is volatile. Retaining Chronicle's data preserves run history,
but recovery also needs the state used to find and tend active runs; durable grain storage alone is not a complete recovery
configuration.

A model that uses something the renderer cannot emit fails the plan with the diagnostic below. Nothing is dropped silently.

| Code | Refused because |
| --- | --- |
| `STAGE-LIFECYCLE-007` | A trigger is a schedule. |
| `STAGE-LIFECYCLE-008` | A transition guard compares something other than a text, number or flag member of the followed read model. |
| `STAGE-LIFECYCLE-010` | A decision, approval, foreach or join; an action whose command has more than its identifier; or a command a state machine sends that has a property the followed read model does not carry with the same name and type. |

The render plan reports one warning per state machine, `STAGE-LIFECYCLE-009`. An event-sourced machine resolves its state from
the followed read model when its grain activates. The generated `ResolveState` returns `InitialState` unconditionally, so the
generated machine starts there on reactivation, even if its read model records another state. Adding a read-model member alone
does not change that generated method; retaining the state requires a resolver that reads it.

Specifications inside a state machine or workflow are not emitted as generated specifications. Before generating an
application, check [Cratis renderer compatibility](renderer-compatibility.md) as well as the lifecycle refusals above.
