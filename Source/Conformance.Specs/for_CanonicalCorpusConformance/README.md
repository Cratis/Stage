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
