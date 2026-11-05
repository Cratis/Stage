---
title: Cratis Stage reference for execution and rendering
description: Find Cratis Stage reference pages for inspecting a running model, checking lifecycle execution, and reviewing generated code and UI render plans.
---

Use the Cratis Stage reference to inspect a running Screenplay model or review what Stage generates from it. A play session
executes supported behavior in a sandbox; generating application code is a separate path with its own supported constructs
and refusal diagnostics. Check the reference for the path you are using before relying on a modeled behavior.

## Topics

### Inspect a running model

- [URLs of a running Stage](urls.md) explains the session's URLs on port `9090`, how module, feature and slice names become routes, and the response envelopes.
- [State machines and workflows](state-machines-and-workflows.md) explains how committed facts drive lifecycles, where to inspect runs, and how execution differs from generated code.
- [How the Stage renders a model](stage-rendering.md) explains how the running session draws screens and connects them to commands and queries.

For startup, port configuration and session limits, continue with [The Stage container](../docker/index.md).

### Review generated artifacts

- [Default generated Scene](default-scene-composition.md) covers editable keyed lookups, omitted cases, input formats and stable proxy identities.
- [Generated values and command responses](generated-values-rendering.md) covers UUID concepts, generation fixtures, response contracts and limits.
- [Event reaction rendering](reaction-rendering.md) covers Automation artifacts, reactor identity, side effects, retry behavior and refusals.
- [Scene UI render plans](render-plans.md) explains what Stage resolves for each `ui profile`, how to interpret findings, and how one invocation covers every target.
- [Template helper catalogue](template-helpers.md) lists the generated v1 locked declarative-kit helper signatures and standard-library allowlist.

To check a model against the renderer's supported surface, continue with [Cratis renderer compatibility](renderer-compatibility.md).
