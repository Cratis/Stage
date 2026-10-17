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

The loader outcome is its own column. A model the loader refuses reads `refused:STAGE-PLAN-004(<issue kinds>)`, for example `refused:STAGE-PLAN-004(UnsupportedQuery+UnsupportedAffectedCardinality)`. The three surfaces then still run on the partial plan, `SemanticExecutionPlan.Compile(model, [])`, so each refuses per artifact instead of hiding behind the loader. Only when even the partial plan does not compile are they `n/a:not-loaded`.

The renderer column alone renames the application. The ESM goldens carry a prose application name (`Canonical Golden Application`), and the Cratis scaffold requires a C# identifier, so it throws on the name before it reads a single construct. The renderer column therefore runs a copy of the model whose application is named after the vector; every other construct is unchanged. The loader, executor and host columns use the model as the corpus ships it.

Every cell is one of:

- `supported`
- `refused:<what>[+<what>]`, where `<what>` is a `STAGE-…` code or `capability:<Name>[<reason key>]` when only the capability types the refusal. The executor appends `@<construct>` (the last eight characters of the construct id) and the host appends `@<construct kind>`. The renderer appends `@<artifact>` with the **full** textual semantic ID from each diagnostic. Distinct artifacts sharing one code remain separate fingerprints; moving or adding a refusal on another artifact changes the cell.
- `n/a:no-specifications`, only for a vector whose plan has no specifications
- `n/a:not-loaded`, when no plan exists at all
- `gap:<what>`, which always fails: an exception, an untyped refusal, a partial result, duplicate, unknown or missing executor result IDs against the plan's exact specification ID set (even without corpus expectations), a corpus expectation that matches no result, or a render that blocked yet emitted artifacts

## Enumeration

Vectors are found by reflection over the corpus assembly: every public static `CanonicalCorpusVector` property, and every full ESM golden (`EsmGoldenVectors.V<n>`, plus the extension vectors). The spec fails if the count drops below `minimumVectorCount`, if `corpusVersion` differs from the resolved package, or if a corpus type yields no vector and is not listed in `excludedTypes` with a reason.

## Every refusal has an issue

Every `refused` cell carries `"issue": <number>`, the Stage issue that tracks lifting that refusal. The spec fails on a refused cell whose `issue` is `null`; `reason` is an optional note. A refusal with no issue is a missing issue, not an exemption: file the issue, then write its number.

### Refusals that are correct by design

The one exception is a refusal that is the right answer because the corpus input is intentionally invalid. Renderer cells list exact `{ "code", "artifact", "reason" }` entries in `byDesign`. The artifact is the full semantic ID, not a kind, name, prefix or shortened hash.

- Every actual renderer code/artifact pair must be covered either by the cell's issue and an exact `{ "code", "artifact" }` entry in `issueRefusals`, or by a `byDesign` entry. An issue number alone does not cover another artifact. When all fingerprints are by design, `issue` is `null`.
- A `byDesign` entry needs a non-empty `reason` and must belong to the harness's explicit invalid-artifact allow-list. The allow-list contains only ESM-019 on `GenerationFixturesCorpus.V21`'s intentionally invalid `TenantOrderId` generation fixture. It does not exempt ESM-019 on any other artifact, and #500 covers only the separately listed ESM-011 render-abort fingerprints.
- Use `byDesign` only when the corpus input is intentionally invalid, such as a fixture that exists to be rejected. A limitation of Stage is never by design; it gets an issue.
- The list can only shrink: an entry whose exact code/artifact pair no longer appears in the actual outcome fails as an unnecessary by-design entry, even when its code still occurs on another artifact. Stale issue-coverage entries fail too.

## Update the table

1. Run the spec. The failure message names each vector, column, expected and actual outcome.
2. If the change is intended, write the actual table: `CRATIS_STAGE_WRITE_CONFORMANCE_MATRIX=/tmp/matrix.json dotnet test Source/Conformance.Specs/Conformance.Specs.csproj --configuration Debug --filter FullyQualifiedName~for_CanonicalCorpusConformance`
3. Copy the file over `conformance-matrix.json`. A cell whose outcome is unchanged keeps its `issue` and `reason`. A new refusal is written with `"issue": null`. Renderer `issueRefusals` and `byDesign` entries are kept only while their exact code/artifact pairs are still in the outcome.
4. Replace each `null` with the tracking Stage issue number (unless every fingerprint is explicitly by design). For renderer cells, also list the exact code/artifact pairs that issue covers in `issueRefusals`. The spec rejects uncovered actual fingerprints.
5. Review the diff. A refusal that became `supported` is the change you wanted to see.
