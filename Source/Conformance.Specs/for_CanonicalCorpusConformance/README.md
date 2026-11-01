# Published C# compile gate (#824)

The renderer cell is `supported` only when planning and the emitted C# compile
without errors. Every published render is compiled, including one already
`refused` by specification refusals. Backend files, `Program.cs`, and Debug
specifications are compiled from their exact bytes without skipping or rewriting.

The in-memory check uses the existing renderer Roslyn setup with `DEBUG` and
pinned client references. It excludes the test host's kernel-only
`Cratis.Chronicle.Core` assembly, which generated applications do not reference.
This is not an MSBuild, generated-spec execution, or kernel run. Exactly one
emitted `.csproj` is required.

A compiler error adds only the first `(code, "compile", emitted relative path)`
fingerprint, sorted by path, line, code, and message. Failure output and the
details sidecar include the line, message, and remaining error count; those are
not matrix identity. A compile record needs a positive tracked `issue`, never
`byDesign`. Diagnostics without a source or in synthetic `GlobalUsings.g.cs`
fail as harness errors rather than becoming tracked refusals.

Capture and regenerate the matrix through
`CRATIS_STAGE_WRITE_CONFORMANCE_MATRIX`, never by editing fingerprints manually.
Rebuild afterward so the reviewed table is copied into the test output. See
[the conformance matrix reference](../../../Documentation/reference/corpus-conformance.md)
for the update procedure and disposition rules.

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
