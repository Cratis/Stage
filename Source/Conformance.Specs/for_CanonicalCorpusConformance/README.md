# Canonical fixture strings (#539)

The renderer surface supplies `given/canonical_strings_catalog.cs` as companion
`.strings` resources with default locale `en`. Its `validation.predicate` key
resolves the `$strings.validation.predicate` rule message in EsmGoldenVectors
V3–V24. The catalog belongs to the harness; production rendering still refuses
`$strings.` messages without a supplied catalog.

# Protected command read race coverage (#557)

`ProtectedCommandReadRaceVectors.Model` has renderer `supported` because the
whole model renders, including the protected `Update` read and its `UniqueValue`
append-time constraint. The cell records render admission, not execution of every
prepared-decision vector.

`when_comparing_protected_read_race_invocations` builds the generated application
and executes nine invocation comparisons against the frozen vector expectations:

- `same-event-other-key`, `other-view-same-key`, `irrelevant-fact-preserved`,
  `no-later-facts`: accepted with the decided value and expected destination.
- `no-fact-command-accept`, the accepted branch of `merged-keys`: accepted without
  command facts.
- `absent-then-created`, `rejected-decision`: the missing-state/requirement
  validation message and no command facts. These compare invocation rejection,
  not settlement against a replaced later world.
- `late-uniqueness-claim`: a competing claim yields `UniqueValue` rejection,
  without command facts.

Prepared-decision replay is **not covered by the renderer comparison**:
`present-state-race-recorded`, `present-state-race-removal`,
`no-fact-command-conflict`, the conflict branches of `merged-keys`, all
`repeat-commit-*` vectors, and `reconstructed-nonprefix-history`. Arc's command API
exposes invocation, not prepare/commit of a reusable decision against alternative
logs. Reexecuting a command would not test those contracts.

The Host/executor remains the reference for all sixteen published vectors.

# Exact captured artifact compilation (#824 Phase A)

The renderer matrix still records **admission only**. A `supported` observation
is not a claim that the generated solution builds or that its specifications run.
`CaptureRenderer` exposes that unchanged observation together with the actual
`ArtifactRenderPlan` and request; existing `Execute` callers keep their observations.

The separate Debug-only `for_RenderCompilation` specs select exactly
`AcmeAdmissionsCorpus.V29` and `EventRoutesCorpus.CompositesV8`, capture each once,
and materialize every captured artifact with the same path and bytes. They inspect
the captured `.slnx` and build that solution in Debug, with no fallback render or
source rewrite. Specification projects must be included. With #753, specifications
live beside slices and the application's Debug project is itself a specification
project; this does not require inventing a separate specs project.

The harness stays on xUnit 2/VSTest. `Xunit` and `Cratis.Specifications` usings in
the generated application are ordinary Debug-only MSBuild items, not generated
global-usings source. Release continues to exclude inline specifications and
their imports. No runner, package or project migration is part of Phase A.

Compilation is cached by the complete ordinal path+byte artifact set (including
project files and binary artifacts), independent of artifact ordering and fixture
name. Concurrent duplicate requests share one task and one compiler invocation;
nonzero outcomes and thrown failures remain cached failures. Each requesting
fixture retains its result mapping. Failures report phase, exit status, timeout,
truncation, duration, project/spec counts, output and the retained log path.
Each new build has a 120-second deadline; the existing generated-application
callers retain their eight-minute default, output caps and artifact integrity
checks.

A separate corruption proof copies the admitted plan and adds
`#error STAGE824_CORRUPTION` to its captured application `Program.cs`, preserving
all other paths and bytes. It requires a nonzero build exit and `CS1029` with
that marker on the corrupted file's diagnostic line. Startup, restore-only,
missing-project, timeout, truncation and integrity failures cannot satisfy it.
This is an intentionally corrupted harness input, not an expected-pass rule for
a production defect.

Phase A claims only exact-source Debug compilation for these two fixtures. It
does not execute the generated specifications, prove their semantic parity,
expand CI/shards/matrices, or resolve #822, #823, #807 or #808.
