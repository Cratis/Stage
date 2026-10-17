---
title: Canonical corpus conformance gate
description: Look up what the corpus conformance spec compares, how to read conformance-matrix.json, and how to update it when a refusal appears or goes away.
---

`Source/Conformance.Specs/for_CanonicalCorpusConformance` runs every vector of the pinned `Cratis.Screenplay.CanonicalCorpus` package through three surfaces and compares each result with a checked-in table, `conformance-matrix.json`. The table is the conformance matrix: a vector without a row fails, and a cell that changes fails until you update the table.

## What each surface measures

| Surface | What runs | Cell is `supported` when |
| --- | --- | --- |
| `executor` | `SemanticSpecificationExecutor` over every specification of the vector | Every specification passes, as the corpus expects |
| `host` | The semantic runtime path only: `SemanticRuntimeAdmission`, read by reflection because it is internal | Every admission entry is supported |
| `renderer` | `CratisRendering.Plan` (planner only, no generated build) | The plan has no error diagnostic |

A model whose execution plan does not compile is refused by the model loader before any surface sees it. All three cells then read `refused:STAGE-PLAN-004`.

Every cell is one of:

- `supported`
- `refused:<CODE>[+<CODE>]`, where each code is a `STAGE-…` code, or `capability:<Name>` when the refusal is typed only by its capability
- `n/a:no-specifications`, for a vector with nothing for the executor to run
- `gap:<what>`, which always fails: an exception, an untyped refusal, a partial result, or a render that blocked yet emitted artifacts

## Enumeration

Vectors are found by reflection over the corpus assembly: every public static `CanonicalCorpusVector` property, and every full ESM golden (`EsmGoldenVectors.V<n>`, plus the extension vectors). The spec fails if the count drops below `minimumVectorCount`, if `corpusVersion` differs from the resolved package, or if a corpus type yields no vector and is not listed in `excludedTypes` with a reason.

## Update the table

1. Run the spec. The failure message names each vector, surface, expected and actual outcome.
2. If the change is intended, write the actual table: `CRATIS_STAGE_WRITE_CONFORMANCE_MATRIX=/tmp/matrix.json dotnet test Source/Conformance.Specs/Conformance.Specs.csproj --configuration Debug --filter FullyQualifiedName~for_CanonicalCorpusConformance`
3. Copy the file over `conformance-matrix.json`. A cell whose outcome is unchanged keeps its `issue` and `reason`. A new refusal is written with a `TODO` reason.
4. Replace each `TODO` with the tracking Stage issue number in `issue`. Use `"issue": null` only with a `reason`. The spec rejects a refusal with neither.
5. Review the diff. A refusal that became `supported` is the change you wanted to see.
