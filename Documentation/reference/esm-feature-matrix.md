---
title: ESM versions and features Stage supports
description: Look up which ESM versions and features Stage runs in the sandbox, which it renders, and which it refuses, with the diagnostic code for each.
---

ESM is the canonical Screenplay executable event-model schema. Each model declares a language and semantic
version pair, such as `37.0/37.0`. Stage audits those pairs one by one. Use this page to see what a model of a
given pair can do in the sandbox (`cratis run`), what the generated-application renderer accepts, and what is
refused. The sandbox runs the model; see [How the Stage renders a model](stage-rendering.md) and
[Cratis renderer compatibility](renderer-compatibility.md) for rendering.

## Audited ESM pairs

Stage admits a model whose language and semantic versions are the same audited pair.

| ESM version | Standing |
| --- | --- |
| 1 to 34 | Audited |
| 35 | Refused |
| 36, 37, 38 | Audited |
| 39 | Refused |
| 40, 41 | Audited |
| 32.x minor versions (for example 32.1) | Admitted with a warning that Stage has not audited the pair |
| Any other version, including 33.x, 34.x and 42 or later | Refused |
| Language and semantic versions that differ | Refused |

ESM 37 is the command clock, 38 adds graphs and 40 adds graph queries. The models of 37, 38, 40 and 41 are
admitted, but Stage checks each member they populate against its audit ledger. A member that is not audited is
handled as described in the next section.

## What the sandbox runs and what it refuses

The sandbox refuses a whole model only when it cannot tell what the model means. Everything else is refused for
the single command, trigger, reaction or specification that needs it, and the rest of the model runs.

| Feature | Sandbox | Renderer | Diagnostic |
| --- | --- | --- | --- |
| Application triggers and the reactions they start | Refused individually, not executed; the rest of the model runs | Refused | `STAGE-ESM-024` |
| Command clock (`SemanticCommand.Calendar`, clock operands, `now` and `today` conditions) | The commands that use it are refused; other commands run | The model is refused | `STAGE-ESM-016` |
| Specification that sets a time zone or uses the clock | Specifications in that slice are refused | The model is refused | `STAGE-ESM-016` |
| Application time zone, context members and declared read model property order | Commands and queries run, with properties ordered canonically; specifications in the affected scope are refused | The model is refused | `STAGE-ESM-016` |
| Graph queries (ESM 40) | Run against Neo4j | Refused | `STAGE-ESM-068` |
| Form field `choose from query` | Kept as a plain input; the gap is reported as a warning | Kept as a plain input; the gap is reported as a warning | `STAGE-SCENE-FORM-004` |
| Unaudited ESM pair or unknown version | The whole model is refused | The whole model is refused | `STAGE-ESM-016` |
| Any other unaudited member, including security-relevant ones | The whole model is refused | The whole model is refused | `STAGE-ESM-016` |

### Application triggers

Nothing in Stage can raise an application trigger yet, so Stage does not execute triggers or the reactions that
they start (`STAGE-ESM-024`). Each is listed as refused with that code; the rest of the model runs. Execution is
tracked in [Cratis/Stage#957](https://github.com/Cratis/Stage/issues/957).

### The command clock

Stage does not run the command clock yet (ESM 37, `STAGE-ESM-016`). A command that uses it is refused when you
execute it, and every other command in the model runs. Specifications cannot run with clock members in their
scope: a clock member in a slice refuses every specification in that slice. A model that uses the clock cannot be
rendered into an application.

Application time zone and context members are application-wide, so every specification is refused while they
are present. A declared read model property order refuses the specifications in that read model's slice.

### Unaudited members

Only the clock members above are refused per command or specification scope. A member Stage has not audited anywhere
else, and any member of an unaudited or unknown version, still refuses the whole model with `STAGE-ESM-016`.
This includes members that affect security, such as authorization. Stage fails closed rather than run a model
whose meaning it cannot check.

### Form pickers

A form field that uses `choose from query` becomes a plain input, because the Scene form field has no place for
the picker yet. When a diagnostics collector is supplied, Stage reports the gap as the warning
`STAGE-SCENE-FORM-004`. The model is not refused.
