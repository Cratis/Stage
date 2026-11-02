---
title: State machines and workflows
description: How the Stage runtime executes Screenplay state machines and workflows on its Orleans silo, how it renders them for a generated application, and what it refuses.
---

A Screenplay `statemachine` says where one thing is in its lifecycle and what entering a state does. A `workflow` is a durable
process: it sends commands, chooses a path, waits for a fact or a person and gives up after a deadline. Both are top level
declarations admitted from ESM v36 (Screenplay 5.1.0), and both mean what the Cratis.Orleans `EventSourcedStateMachine` and the
durable workflow engine mean. Stage runs them on the Orleans silo it hosts ([ports and clustering](../docker/index.md)) and
renders them for the applications it generates.

## What the runtime does

When Stage performs a model, the semantic runtime keeps the world of facts. A state machine and a workflow follow that world.

| Construct | Runtime behavior |
| --- | --- |
| State machine | Derived from the committed facts with the same rule the specifications use: the first transition on the event, in declaration order, whose guard holds over the followed read model and whose target the current state allows. Anything else is ignored. |
| `entering send <Command>` | The runtime sends the command through the semantic runtime. The subject key becomes the command's identifier and every other property takes the member of the followed read model with the same name. |
| `entering start <Workflow>` | The runtime admits a run of the workflow on the silo. |
| Workflow | Each workflow becomes a definition snapshot served by an `IWorkflowDefinitions` source built from the loaded model. Actions send their command, decisions evaluate their condition over the current subject, timers and expiries run on the engine, and runs are durable on the silo. |
| Event trigger, state trigger | A fact the trigger names, or a machine entering the state it names, admits a run. A repeated delivery finds the run it started. |
| `wait ... for <Event>` | The fact resumes the runs waiting at the node for the same subject. |
| `approval` | The run is suspended until a person decides it. |

Only facts committed after the runtime started act. History is never replayed into side effects, so a restart does not send a
command twice.

### Where a state machine is

```bash
curl http://localhost:8080/stage/lifecycles
```

reports, for each state machine, the state of every instance it has seen, and for each workflow the runs the sandbox started with
the nodes they are suspended at. Anything the runtime refuses is listed under `refused`.

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
It never blocks the rest of the model, and its specifications are still verified.

| Code | Refused because |
| --- | --- |
| `STAGE-LIFECYCLE-001` | A workflow declares more than one trigger. The engine runs a graph with exactly one trigger node; write one workflow per trigger. |
| `STAGE-LIFECYCLE-003` | A foreach sits inside the branches of another foreach. |
| `STAGE-LIFECYCLE-004` | A foreach is not gathered by exactly one join. |
| `STAGE-LIFECYCLE-005` | A join does not gather the branches of a foreach. |
| `STAGE-LIFECYCLE-006` | An edge leaves by an outlet the engine node does not have. |
| `STAGE-LIFECYCLE-007` | A trigger is a schedule. |

## Specifications

The specifications written inside a state machine or a workflow are verified by the spec runner with the others, through the
reference lifecycle evaluator. Select them with the application scope, or with no scope at all; a module, feature or slice scope
leaves them out.

## What the renderer emits

A rendered application gets, in its `Lifecycles` namespace:

| Model | Generated code |
| --- | --- |
| `statemachine` | An `EventSourcedStateMachine<TModel>` grain with a nested `State<TModel>` per state, `AllowedTransitions`, the transitions `On<TEvent>().When(...).TransitionTo<TState>()` in declaration order, and an `OnEnter` that sends the command through `ICommandPipeline` or starts the workflow. |
| `workflow` | A definition class implementing `IWorkflowDefinitions`, one performer per command the workflows send, a reactor that signals the waits for each event and `GeneratedWorkflows.Configure`, which registers the event triggers. |

The generated `Program.cs` passes `GeneratedWorkflows.Configure` to `AddEventSourcedWorkflows` when the model has an event trigger.

A model that uses something the renderer cannot emit fails the plan with the diagnostic below. Nothing is dropped silently.

| Code | Refused because |
| --- | --- |
| `STAGE-LIFECYCLE-007` | A trigger is a schedule. |
| `STAGE-LIFECYCLE-008` | A transition guard compares something other than a text, number or flag member of the followed read model. |
| `STAGE-LIFECYCLE-010` | A decision, approval, foreach or join; an action whose command has more than its identifier; or a command a state machine sends that has a property the followed read model does not carry with the same name and type. |

The rendered application reports one warning per state machine, `STAGE-LIFECYCLE-009`. Cratis.Orleans resolves the state of an
event-sourced machine from the followed read model alone when its grain activates again. A model that declares no member recording
the state therefore resumes in its initial state after a restart. Add a read model member the projection sets on each transition if
the state must survive.

The specifications inside a state machine or a workflow are not rendered as generated specifications yet.
