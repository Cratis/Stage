---
title: Rendering gaps and placeholders
description: How Stage reports unsupported event members and the owners omitted with them.
---

# Rendering gaps and placeholders

An unsupported member of an event can be rendered around when Stage can prove which
owners depend on that event. The plan is **Degraded**, not Complete. Stage emits no
artifact whose semantic sources include an omitted owner; it does not emit a stub
for the unsupported event.

The root event carries `STAGE-DEGRADE-001`. Its warning preserves the original
`STAGE-ESM-012` message. Each omitted dependent carries `STAGE-DEGRADE-005` and names
the root event or events that caused its omission. The selected constructs appear
in the semantic inventory as `GraphProvenOmittedLeaf` placeholders. Report format
version 1 is unchanged.

This behavior requires a proven dependency closure. Incompleteness is tolerated
only for unknown variants or unresolved identities in a root event's own property
type subtree, at the explicitly supported property-type traversal locations. A gap
on another owner, an unclassified member, or ambiguous ownership still refuses the
whole render. A failure while rendering the remaining owners also restores the
original whole-render refusal.

Strict mode always retains the whole-render refusal. To remove the gap, upgrade
Stage to support the member, or change the event member to a supported type.
