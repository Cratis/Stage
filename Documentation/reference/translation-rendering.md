---
title: Translation rendering
description: The supported inbox capture and public-event outbox publication subset, and its remaining refusals.
---

Stage renders supported `Translate` slices as Chronicle reactors. Inbound translation turns foreign
facts into local private facts; outbound translation publishes a public contract to the outbox.
This describes generated applications, not the live semantic sandbox or the specification executor.

## Contracts and routing

| Modeled construct | Generated backend |
| --- | --- |
| Private event | `[EventType]`; private remains the default |
| Local public event participating in a supported translation | `[EventType]` and `[Public]` |
| Foreign event participating in a supported translation | `[EventType]` and `[EventStore("<origin>")]`; the origin string is retained verbatim |
| Inbound events-source capture | A reactor per capture, subscribed to the foreign origin's inbox, with a handler per source contract |
| Capture append | An `EventForEventSourceId` envelope containing the local private fact; the capture key supplies its destination |
| Outbound event-target projection | A reactor per projection, observing private events and appending the public target to the outbox |

A capture's reactor uses its origin store to select `inbox-<origin>`. It never produces the foreign
contract or appends that contract to the local event log. Chronicle's event result handler appends the
returned local envelopes, including their contract and append tags. Publication writes to the outbox,
not the local event log.

## Inbound subset

A supported capture has an events source, one foreign origin, a key on every source contract, and
local private append targets. Field mappings read the incoming contract directly; literal mappings
retain their modeled types. A mapped field must exist on every source contract. Capture `map`, children,
nested scopes and mappings needing an accumulated record overlay remain refused.

Unconditional appends produce a local fact for each received occurrence. For `when added`, the generated
handler checks for an existing local fact of the target event type and capture key before returning an
append. This is an **approximation**, not persistent capture-engine state: another producer's matching
fact can suppress the append, and concurrent or retried deliveries are not an exactly-once guarantee.
`changed`, transition, expression, removal and other capture conditions remain refused by name with
`STAGE-ESM-024`; Stage does not replace them with unconditional appends.

## Outbound subset

A supported event-target projection publishes a local public event from private source events. Its scope
is per event source, its key is the event-source identity, and its top-level mappings use `set` with a
source property, literal or event-source identity. `every`, joins, child scopes, composite or custom keys,
`clear`, non-set operations and event-target reducers remain refused.

Before publishing, the reactor reads the last public fact of the target type for the same event source
from the outbox. It appends only when the mapped public record differs. Identical consecutive records
are suppressed across reactor reactivation; another key remains independent, and a change back to an
older value is published again. This uses durable outbox history, not process-local state. An unsuccessful
outbox append fails the handler. This comparison does not make the private-log append and publication
one transaction or provide concurrent exactly-once delivery.

## Diagnostics and specifications

| Diagnostic | Refused shape |
| --- | --- |
| `STAGE-ESM-024` | Unsupported Translate shape or capture condition; external-system captures, record overlays and mixed translation/command or read surfaces |
| `STAGE-ESM-032` | Public contracts outside the supported translation subset; unsupported event-target projections and event-target reducers |
| `STAGE-ESM-033` | Foreign contracts outside the supported subset, including a capture spanning several origin stores |
| `STAGE-ESM-011` | Translate specifications outside the isolated handler-fixture subset described below |

A direct `when append` can render a `Specification` that constructs and calls the generated
reactor handler. Inbound fixtures provide explicitly keyed private history for `when added`
and assert the captured `EventForEventSourceId` sequence: count, type, payload, and source id.
Outbound fixtures provide explicitly keyed private-log history and assert the actual public
objects appended to the outbox, including their payloads and source ids. Public outbox history
is not yet supported in generated specifications. Explicit `then none`
is a zero-output assertion, not a skipped check.

These are isolated handler tests, not running Chronicle subscriptions. They do not prove inbox
transport, checkpointing, or replay delivery. Input fan-out, downstream captures/publications/
reactions, append-time constraints, history that triggers settlement, and recovery/redelivery
remain explicit `STAGE-ESM-011` refusals. Expected outcomes must belong to the selected handler;
a public outcome from another slice is not approximated as a local append. Unordered outcomes,
caller/clock/error/return assertions, and values outside the existing typed event-fixture subset
are also refused.

With render-around enabled, Stage omits refused specifications, reports their identities in
`Refusals`, and can still render otherwise admitted translation artifacts. Without that opt-in,
the refusal blocks the plan.

See [Build a renderer target](../guides/build-renderer-target.md) for the admission table and
[Event reaction rendering](reaction-rendering.md) for Automation reactors.
