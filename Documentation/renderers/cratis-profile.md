---
title: Cratis renderer profile
description: How the Cratis renderer handles unsupported event property members.
---

# Cratis renderer profile

When rendering around refusals, the Cratis renderer can omit an event with an
unsupported property member and the owners proven to depend on it. Independent,
supported slices still render. The resulting plan is **Degraded**.

Only event-owned failures from the owner-scoped `STAGE-ESM-012` path qualify. This
does not enable rendering around arbitrary command, query, reaction, or shared-type
errors. When a planned migration removes files for a read model in the omission
closure, the renderer retains the whole-render refusal.

Strict mode refuses the whole render and emits no artifacts. Normal supported
models keep their existing output. See [Rendering gaps and placeholders](gaps-and-placeholders.md)
for root and dependent diagnostics, inventory placeholders, and remedies.
