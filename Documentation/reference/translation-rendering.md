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
local private append targets. Field mappings read the key's capture state, which overlays every source
contract seen for that key, so a mapped field must exist on at least one source contract and have the
target property's type; Stage does not coerce field values. Literal mappings retain their modeled types.
Capture `map`, children and nested scopes remain refused.

### Capture state

The generated inbox reactor decides every guard from the key's previous capture state, the same state
the reference evaluator keeps (Screenplay decision 0080):

- The first record of a key is `added`; `changed` and transition guards never hold for it.
- A record whose overlaid state equals the previous state appends nothing, guarded or not.
- `any changed`, `all changed` and `from ... to ...` compare the previous and current state of the key.
- `when removed` never holds for a capture over events, so the target is not appended.

The reactor rebuilds the previous state from the inbox it observes: it reads the captured foreign
event types from that inbox, keeps the records before the delivered event's sequence number whose
capture key matches, and overlays them in order. The inbox is append-only and the read is bounded by
the delivered position, so a retry, a crash redelivery or a replay of the same event computes the same
state and the same effects.

Two alternatives were rejected. Private output history (the local facts already appended for the key)
is not capture state: another producer's fact suppresses an `added`, and a capture with no output for a
record cannot remember that record. A generated read model of the last-seen record per key is
eventually consistent with the reactor that reads it, so a delivery can observe a model that has not
yet seen the previous record, and replay rebuilds it independently of the reactor. Chronicle's client
capture builder reads external systems, not inbox events, so it does not fit an events source.

The cost is one inbox read per delivery that grows with the inbox length. The reactor remains
`[OnceOnly]`, so a full observer replay does not re-append. Each local effect carries a Chronicle
idempotency key derived from the reactor delivery, capture id and modeled append position. Retrying
that delivery after a partial append, a lost acknowledgment or an append followed by a lost checkpoint
uses the same keys: already-appended effects succeed without appending duplicates. Different effects
and different deliveries have different keys, even when an earlier append's guard produces no effect.

This requires Chronicle client and kernel **19.99.1 or later** with idempotent appends. The generated
application pins that kernel image. A kernel without the capability is refused; Stage does not fall
back to unkeyed appends. This protects capture effects in the destination event log, not arbitrary
external side effects. Publication, capture state reconstruction and replay exclusion are unchanged.

Guards whose result Stage cannot reproduce exactly are refused by name with `STAGE-ESM-024`:
expression guards, guard paths with `.` traversal, guards over list or record fields, and transitions
over values whose text form is not text, a number or a boolean. Stage does not replace a refused guard
with an unconditional append.

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
| `STAGE-ESM-024` | Unsupported Translate shape or capture guard; external-system captures, guards Stage cannot reproduce exactly, mapped fields of another type, and mixed translation/command or read surfaces |
| `STAGE-ESM-032` | Public contracts outside the supported translation subset; unsupported event-target projections and event-target reducers |
| `STAGE-ESM-033` | Foreign contracts outside the supported subset, including a capture spanning several origin stores |
| `STAGE-ESM-011` | Translate specifications outside the isolated handler-fixture subset described below |

A direct `when append` can render a `Specification` that constructs and calls the generated
reactor handler. Inbound fixtures place given foreign records in the inbox before the delivered
event, so first observation, unchanged suppression and changed records are exercised through the
reactor's own state read, and assert the captured `EventForEventSourceId` sequence: count, type,
payload, and source id. A fixture registers only the contracts its selected handler resolves.
Outbound fixtures provide explicitly keyed private-log history and assert the actual public
objects appended to the outbox, including their payloads and source ids. Public outbox history
is not yet supported in generated specifications. Explicit `then none`
is a zero-output assertion, not a skipped check.

These are isolated handler tests, not running Chronicle subscriptions. Capture specifications pass
Chronicle delivery identity to the generated handler. They do not prove inbox transport,
checkpointing, or replay delivery. Input fan-out, downstream captures/publications/reactions,
append-time constraints, history that triggers settlement, and authored recovery/redelivery
specifications remain explicit `STAGE-ESM-011` refusals. Expected outcomes must belong to the selected handler;
a public outcome from another slice is not approximated as a local append. Unordered outcomes,
caller/clock/error/return assertions, and values outside the existing typed event-fixture subset
are also refused.

With render-around enabled, Stage omits refused specifications, reports their identities in
`Refusals`, and can still render otherwise admitted translation artifacts. Without that opt-in,
the refusal blocks the plan.

See [Build a renderer target](../guides/build-renderer-target.md) for the admission table and
[Event reaction rendering](reaction-rendering.md) for Automation reactors.
