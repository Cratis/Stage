---
title: Canonical corpus conformance gate
description: Look up what the corpus conformance spec compares, how to read conformance-matrix.json, and how to update it when a refusal appears or goes away.
---

`Source/Conformance.Specs/for_CanonicalCorpusConformance` runs every vector of the pinned `Cratis.Screenplay.CanonicalCorpus` package through the strict model loader and three surfaces, and compares each result with a checked-in table, `conformance-matrix.json`. The table is the conformance matrix: a vector without a row fails, a row missing a column fails, and a cell that changes fails until you update the table.

## What each column measures

| Column | What runs | Cell is `supported` when |
| --- | --- | --- |
| `loader` | `SemanticExecutionPlan.Compile(model)`, the strict load the model loader applies | The plan compiles with no issues |
| `executor` | `SemanticSpecificationExecutor` over every specification of the vector | Every specification passes, as the corpus expects |
| `host` | `SemanticRuntimeAdmission` (internal; the Host grants this assembly `InternalsVisibleTo` in Debug) | Every admission entry is supported |
| `renderer` | `CratisRendering.Plan` (planner only, no generated build) | The plan has no error diagnostic |

The loader outcome is its own column. A model the loader refuses has outcome `refused` and one record per distinct `(STAGE-PLAN-004, issue kind, full artifact identity)`. Several unsupported validation rules or projection transitions can share that identity. The loader uses set semantics: it keeps one fingerprint and writes its occurrence count plus all distinct details, ordered ordinally, to the details sidecar. The three surfaces still run on the partial plan, `SemanticExecutionPlan.Compile(model, [])`, so each refuses per artifact instead of hiding behind the loader. Only when even the partial plan does not compile are they `n/a:not-loaded`.

The renderer column alone renames the application. The ESM goldens carry a prose application name (`Canonical Golden Application`), and the Cratis scaffold requires a C# identifier, so it throws on the name before it reads a single construct. The renderer column therefore runs a copy of the model whose application is named after the vector; every other construct is unchanged. The loader, executor and host columns use the model as the corpus ships it.

Every cell is one of:

- `supported`
- `refused`, with a `refusals` array containing the exact fingerprints and dispositions described below
- `expected-refusal`, when the corpus itself expects an unsupported executor result; these cells are exempt from tracking and must carry no records
- `n/a:no-specifications`, only for a vector whose plan has no specifications
- `n/a:not-loaded`, when no plan exists at all
- `gap:<what>`, which always fails: an exception, an untyped refusal, a partial result, duplicate, unknown or missing executor result IDs against the plan's exact specification ID set (even without corpus expectations), a corpus expectation that matches no result, or a render that blocked yet emitted artifacts

## Enumeration

Vectors are found by reflection over the corpus assembly: every public static `CanonicalCorpusVector` property, and every full ESM golden (`EsmGoldenVectors.V<n>`, plus the extension vectors). The spec fails if the count drops below `minimumVectorCount`, if `corpusVersion` differs from the resolved package, or if a corpus type yields no vector and is not listed in `excludedTypes` with a reason.

## Every refusal has one disposition

A `refused` cell has one record per fingerprint in `refusals`. Each record contains:

| Field | Contract |
| --- | --- |
| `code` | A typed `STAGE-…` code, or `capability:<Name>[<reason key>]` when only the capability types the refusal |
| `kind` | Loader plan-issue kind or Host admission-entry kind; omitted for executor and renderer |
| `artifact` | Full artifact identity, never a shortened hash, kind, name, prefix, or wildcard |
| `issue` | Positive integer identifying the Stage issue that tracks this exact refusal |
| `byDesign` | Non-blank reason for an explicitly allowed invalid fixture, instead of `issue` |

The executor collapses repeated refusals across specifications only when they share the same code and full construct identity. The Host keeps every artifact and kind, including separate specification artifacts. Renderer fingerprints retain the full semantic ID from each diagnostic.

Matching is ordinal and exact on `(vector, surface, code, kind, artifact)`. A record on another vector or surface grants no coverage. Changing a code, kind, or artifact reports both `unrecorded-refusal` and `stale-record`; duplicates report `duplicate-record`. Empty or wildcard record artifacts report `code-only-record`. Missing observed artifacts report `refusal-without-artifact`.

Every record needs exactly one disposition. A null or non-positive issue without a reason reports `missing-disposition`; supplying both fields reports `conflicting-disposition`. Issue 500 may track only `STAGE-ESM-011`. Records on supported, n/a, gap, or expected-refusal cells fail as `records-on-unrefused-cell`.

The loader rejects legacy cell-level `issue`, `issueRefusals`, and `byDesign` fields, unknown fields, duplicate fields, and non-integer issue values with schema diagnostics. There is no compatibility fallback to cell-level ownership.

### Refusals that are correct by design

`byDesign` is allowed only for the renderer's `STAGE-ESM-019` on `GenerationFixturesCorpus.V21`'s intentionally invalid `TenantOrderId` fixture, artifact `sem1:25bdf5d2a8f97601c41d6bc30d4da1d2fd4123e1ebf894d79f96b9103b85caf4`. It requires a non-blank reason. Another artifact, vector, or surface reports `unauthorized-by-design`, even if the code is identical. A Stage limitation is not an invalid fixture; it needs an issue.

When an exempt artifact no longer appears, its record becomes stale and must be removed.

## Update the table

1. Run the Debug specs. Failure messages name the vector, surface, and affected fingerprint or outcome.
2. Capture the actual table into an existing ignored work directory:

   ```bash
   CRATIS_STAGE_WRITE_CONFORMANCE_MATRIX="$PWD/.ai-work/matrix.json" dotnet test Source/Conformance.Specs/Conformance.Specs.csproj --configuration Debug --no-build --filter FullyQualifiedName~when_running_every_canonical_vector
   ```

   This writes `matrix.json` and `matrix.details.json`. The sidecar contains the full observed details for diagnosis; details never enter the matrix. The run still validates the checked-in expectations, so writing an actual table is not a passing verdict.
3. Review changed outcomes and fingerprints before replacing `conformance-matrix.json`. The writer carries a disposition only from one identical prior `(vector, surface, fingerprint)` record. A new or ambiguous fingerprint gets `"issue": null`, even if another artifact has the same code or the cell outcome is unchanged.
4. Replace each null with the issue that covers that exact refusal, or the authorized by-design reason. Do not infer coverage from another record in the cell.
5. Rebuild to copy the reviewed matrix into the test output directory, then rerun the specs. Removed refusals must leave no stale records.
