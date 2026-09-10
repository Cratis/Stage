---
title: The specification runner
description: Running the specifications modeled in a Screenplay model as a container job, and reading the results.json it writes.
---

The specifications in a Screenplay model are part of the model, not a separate test project — so they can be run
without building anything. `cratis/stage-specrunner` is that run: a **run-to-completion job** that compiles the
model, verifies its specifications, writes `results.json`, and exits.

Unlike [the Stage container](index.md), it starts no server and needs no event store. It is meant for a build
pipeline or an editor's "verify my model" action.

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
| `--slice <guid>` | — | Limit the run to a single slice. |
| `--spec <guid>` | — | Limit the run to a single specification. |

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
