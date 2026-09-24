---
title: The specification runner
description: Running the specifications modeled in a Screenplay model as a container job, and reading the results.json it writes.
---

The specifications in a Screenplay model are part of the model, not a separate test project — so they can be run
without building anything. `cratis/stage-specrunner` is that run: a **run-to-completion job** that compiles the
model, verifies its specifications, writes `results.json`, and exits.

Unlike [the Stage container](index.md), it starts no server and needs no event store. It is meant for a build
pipeline or an editor's "verify my model" action. The default structural engine is **deprecated**: it checks
model consistency, not behavior, and will be removed in the next major version. This minor version retains
`structural` as the default for existing `results.json` consumers; each structural run prints one deprecation
notice to standard error. Migrate to `--engine semantic` and the semantic report format described below.

```bash
docker run --rm \
    -v /path/to/screenplays:/model:ro \
    -v /path/to/results:/output \
    cratis/stage-specrunner:latest
```

```text
Ran 3 specification(s) for event model 'Invoicing'. Results written to /output/results.json.
```

## Mount points and arguments

The image defaults to the two mounted folders, so the invocation above needs no arguments:

| Argument | Default | Meaning |
|---|---|---|
| `--model <file-or-folder>` | `/model` | One `.play` file, or a folder searched recursively for `.play` files and compiled as one application. |
| `--output <file>` | `/output/results.json` | The file the results are written to. |
| `--slice <guid>` | — | Limit the structural run to a single slice (structural-only). |
| `--spec <guid>` | — | Limit the structural run to a single specification. |
| `--engine semantic` | `structural` | Opt in to executable semantic specifications and the versioned semantic report. |
| `--specification <semantic-id>` | — | Select a semantic specification (semantic engine only). |
| `--scope <semantic-id>[,<semantic-id>...]` | — | Select application, module, feature, slice or specification identities (semantic engine only). |
| `--catalog <file>` | — | Use an authoritative semantic identity catalog (semantic engine only). |
| `--application <name>` | Input folder/file name | Preserve the application name across input layouts (semantic engine only). |

Override either default by passing arguments after the image name. To compile just one file, mount only that
file read-only; no host parent-folder mount is needed:

```bash
docker run --rm \
    -v /path/to/invoicing.play:/model/input.play:ro \
    -v /path/to/results:/output \
    cratis/stage-specrunner:latest \
    --model /model/input.play --output /output/invoicing.json
```

File extensions are case-insensitive. A single-file input does not load sibling `.play` files. Implementation
`file` references remain symbolic and are not opened or executed by this loader. For local use, the executable
requires both `--model` and `--output`; the defaults above are supplied by the container.

## Exit codes

| Code | Meaning |
|---|---|
| `0` | The run completed and `results.json` was written. **A failing specification is still a completed run** — read the outcomes from the file. |
| `1` | The input path is missing, the file extension is unsupported, the folder is empty, or model compilation fails. An actionable error is written to standard error. |
| `2` | A required argument (`--model` or `--output`) was missing. A usage line is written to standard error. |

Input failures do not write results or delete or overwrite an existing output file. Compiler errors identify the
source path and position, ordered by path, line and column. Folder diagnostics use relative source paths.

The existing argument parser treats an invalid `--slice` or `--spec` GUID as no filter, rather than rejecting it.
Check filter values before invoking the runner; this input-path support does not change that policy.

## Reading results.json

The file carries the model's identifier and one result per specification, each with its per-step outcomes:

```json
{
  "eventModelId": "cacf0ce6-f6bc-9300-c909-657aa5b1cbb8",
  "results": [
    {
      "sliceId": "9f877a6c-f978-e3c5-f3d4-b6d23a0bc11c",
      "specificationId": "22719326-4950-add1-7947-98eee1a11d2a",
      "specificationName": "RegisteringADraftInvoice",
      "sliceType": "StateChange",
      "outcome": "Failed",
      "steps": [
        {
          "kind": "Given",
          "title": "Given 1 event(s)",
          "outcome": "Failed",
          "message": "One or more Given events do not exist on the slice.",
          "differences": [
            { "path": "CustomerRegistered", "expected": "an event on the slice", "actual": "not found" }
          ]
        }
      ],
      "note": "State change: verified the When command, the Given/Then events resolve to the slice, …"
    }
  ]
}
```

- **`outcome`** is `Passed`, `Failed`, or `Inconclusive` — the last meaning the step could not be verified for that
  slice type yet, which is not a failure.
- **`steps[].kind`** follows the specification's own shape (`Given`, `When`, `ThenEvents`, `ThenErrors`, …), so a
  failure points at the clause that disagreed rather than at the specification as a whole.
- **`differences`** carries the expected/actual pairs behind a failure, addressed by `path`.
- **`note`** states what was verified for that slice type — and, just as importantly, what was not.

:::note
Verification is **model-level**: the runner checks a specification against the model it belongs to — that the
events and commands it names resolve to the slice, and that the modeled rules and expected errors agree.
Executing the slice behaviorally against a live Chronicle is a follow-up, and each result's `note` says so.
:::

The types behind the file live in `Cratis.Stage.Contracts` (`SpecificationRunResults`,
`SpecificationRunResultsFile`), so tooling should deserialize with those rather than reading the JSON by hand.

## Opt in to semantic execution

The semantic engine is opt-in in this minor version. Use `--engine semantic` for behavioral execution rather than the deprecated structural check:

```bash
dotnet run --project Source/SpecRunner -- \
    --engine semantic --model /path/to/Projects.play \
    --output /path/to/semantic-results.json \
    --application Projects
```

The output is **not** the legacy Guid-keyed `results.json`. Read it with
`SemanticSpecificationRunReportFile`: schema `stage-spec-run/1` contains semantic IDs, a revision, and
per-spec `Passed`, `Failed`, `Unsupported`, or `Cancelled` outcomes. An unsupported capability carries its
kind, the offending semantic construct ID and a reason. Do not feed this file to a legacy structural-results
reader. The process returns `0` after writing the report even when a specification failed or was blocked;
read its outcomes. Invalid semantic IDs, engine names, or structural-only flags with `--engine semantic` return `2`.
Missing or invalid catalog/model inputs return `1` with a diagnostic.

The semantic engine runs admitted commands through Arc's in-memory command pipeline, with a fresh Chronicle
in-memory event log per specification. It supports explicit-source Given events and direct `when appended`
events, unconditional command production with literal or command-property mappings, and event assertions
(including any-order assertions). It evaluates portable command authorization before dispatch: `given caller`
sets an Arc principal, while authenticated, role, claim (literal, subject or artifact), and/or policies follow
Screenplay's reference semantics. Nested artifact paths cannot reach composite values, because composite command values are not admitted. `then denied` checks Unauthorized and appends no new facts. Roles and claims
remain distinct; a role-URI claim is blocked as `Unsupported(Authorization)` rather than treated as a role.

Command rules (`NotEmpty`, `Minimum`, `Maximum`, `Equal`, `NotEqual`, `GreaterThan`,
`GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`, `Length`, `Matches`), scalar concept rules,
and command requirements reject on every failed severity (including warning and information). The first
rejection message follows reference order: command rules, concept rules, requirements; authored messages
and Screenplay's default messages are preserved. Collection-only rule kinds and non-scalar values remain
unsupported. Unique-value and unique-event constraints are checked before append, including prior Given
facts, releases, casing and replacement within a batch; a violation is an atomic rejection with the
constraint name in `trace.rejectionCode` and the reference message in `trace.rejection`. Direct append enforces the same constraints but
does not run command authorization or validation. Conflict outcomes cannot be reached from these admitted
specifications: the Screenplay v4.24.0 reference evaluator declares `SemanticConflict` but never produces it.

Facts are compared against Stage's recorded occurrences; Chronicle's persisted log is checked for fact count.
The clock is injectable through the in-process API. The tenant and identity allocator options are reserved
and do not affect this run; implicit identity allocation remains unsupported.

Read-model assertions, seeded read models, keyed queries, and specifications whose Given, produced or directly
appended events feed a projection (including scoped projections) return typed `Unsupported` before execution: Chronicle 19.4.7
does not offer per-run projection execution through its public scenario APIs. Other
unimplemented behavior (conditional production, implicit event-source identity allocation,
external effects and unsupported expression or value shapes) is also blocked rather than reported as a
pass. Run the semantic executor in its **own process**: Arc's scenario replaces the process-wide
`Internals.ServiceProvider` and leaves it pointing at a disposed provider. Serializing calls does not protect
another host in the same process; isolation is required until Arc offers a scoped alternative.
This is not a live Chronicle integration check. Keep the structural engine for existing Studio jobs
until consumers explicitly migrate to the new report schema.
