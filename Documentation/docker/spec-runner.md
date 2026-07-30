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
    -v /path/to/screenplays:/model \
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
| `--model <folder>` | `/model` | The folder of `.play` files. Searched recursively; every file beneath it is compiled and merged into one model. |
| `--output <file>` | `/output/results.json` | The file the results are written to. |
| `--slice <guid>` | — | Limit the run to a single slice. |
| `--spec <guid>` | — | Limit the run to a single specification. |

`--model` takes the **folder**, not a file. Override either default by passing the arguments after the image
name:

```bash
docker run --rm -v "$PWD":/model -v "$PWD/out":/output cratis/stage-specrunner:latest \
    --model /model --output /output/invoicing.json --slice 9f877a6c-f978-e3c5-f3d4-b6d23a0bc11c
```

## Exit codes

| Code | Meaning |
|---|---|
| `0` | The run completed and `results.json` was written. **A failing specification is still a completed run** — read the outcomes from the file. |
| `2` | A required argument (`--model` or `--output`) was missing. A usage line is written to standard error. |

A model that does not compile, or a `--model` folder holding no `.play` files, fails the run with an error
describing the offending file and position rather than a results file.

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
