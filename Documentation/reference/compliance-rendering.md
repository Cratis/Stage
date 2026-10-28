---
title: Concept compliance rendering
description: Concept protection in ESM v13 generated applications, protected identity refusals, and default-on syntax-renderer compliance details.
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Stage renders compliance markers on a concept's generated `ConceptAs` record. In generated event
payloads, Chronicle applies these markers to scalar concepts, collection elements, and concept-valued
members inside composite records. Personal data uses encryption and erasure; operational secrets use
encryption without erasure.

## Executable semantic models

Stage admits exact ESM v1–v16 language/semantic pairs from Screenplay 4.154.0. The semantic planner maps
`SemanticConcept.Compliance` to these attributes, written by their short names with the `using` directives they need
(`Cratis.Chronicle.Compliance.GDPR`, `Cratis.Chronicle.ProtectedValues`, `Cratis.Arc.Chronicle.Commands`):

| Concept classification | Generated attributes |
| --- | --- |
| `pii` or `personal` | `[PII]` |
| `secret`, unspecified or subject scope | `[Encrypted]` + `[NotAudited]` |
| `secret scope namespace` or `global` | `[Encrypted(EncryptionScope.Namespace)]` (or `.Global`) + `[NotAudited]` |
| `pii secret` | `[PII]` only; personal classification takes precedence. |

The semantic path never emits `[ComplianceDetails]`. GDPR special-category and criminal qualifiers
are carried by ESM v13 but do not change the generated attribute. Reasons are authoring metadata,
not ESM members. Compliance details are a syntax-renderer feature, on by default there ([Stage #259](https://github.com/Cratis/Stage/issues/259)).
Unmarked models retain their earlier ESM version and byte-identical artifacts.

The semantic renderer and Host admit text and non-text `secret` concepts: `Int`, `Decimal`,
`Bool`, `Date`, `DateTime`, and `Uuid` retain their primitive types. Chronicle 19.59.6 encrypts
these values at rest and releases them in their original shape. `pii secret` still uses PII
protection only.

On the semantic path, each command input that transitively contains any compliance-marked concept
(`pii`, `secret`, or both) also gets
`[property: NotAudited]`. This uniform rule includes
scalar inputs, optional inputs, collections, and nested composites. Arc omits the entire input from
causation, including its unprotected members; unrelated inputs remain auditable. The concept-level
attributes remain in place, and they protect different things: `[Encrypted]` protects the value in stored
event payloads. `[NotAudited]` keeps a command input out of Arc's command causation chain and does nothing
else; it does not protect event payloads or logs. `[PII]` encrypts event values and also withholds command
inputs from causation.

Arc 22.87.0 withholds scalar `[PII]` concept inputs through Chronicle's `PIIMetadataProvider`.
That check examines the command property, its declaring type, its direct property type, and its
constructor parameter, but does not recursively inspect composite members or collection elements.
Stage's property-level exclusion covers those nested personal values and secrets, and is also emitted
for scalar PII inputs for consistency. Event-payload encryption alone does not protect causation
values. The syntax renderer retains its existing concept-level attributes; it does not add the
semantic path's transitive input exclusions.

## Protected identities and Play

The semantic renderer refuses compliance on command or read-model identifiers, event destinations,
projection or removal keys, event-source or stream identities, and enum concepts with `STAGE-ESM-040`.
It checks the whole application before emitting any artifacts. The syntax renderer retains
`STAGE-CRATIS-COMPLIANCE-001` for protected event-source identities. Use an unmarked UUID surrogate
identity and keep the protected natural value as a separate property; do not remove its classification.

In v13 the data subject is the event's event source. Explicit subject lineage is not rendered
([Stage #28](https://github.com/Cratis/Stage/issues/28)); Screenplay refuses personal or subject-scoped
secret values together with an explicit event `subject` mark.

The semantic Stage Host registers personal-data and secret concept metadata with Chronicle, which
encrypts event values at rest and releases them on read, like rendered applications. Protection follows
concept values through collection elements and composite members. The Host appends through
`IChronicleClient` (`ProducedEventAppender`), not through Arc's command pipeline, so Arc's `[NotAudited]`
plays no part there. Personal-data erasure uses Chronicle's encryption-key deletion
keyed by the event source; explicit subject lineage remains [Stage #28](https://github.com/Cratis/Stage/issues/28).
Existing Play stores retain their earlier plaintext events; the new metadata does not retroactively
encrypt them. Passing reference or Stage specifications proves modeled outcomes, not storage protection.

## Syntax renderer details

The syntax renderer always honors Screenplay secret encryption scope in generated C# attributes. Namespace and global secrets emit `[Encrypted(EncryptionScope.Namespace)]` and `[Encrypted(EncryptionScope.Global)]` even with details disabled. An unspecified or subject scope keeps the bare `[Encrypted]` output when details are disabled. Personal data remains `[PII]`, including `pii secret` concepts whose secret scope is ignored.

The `complianceDetails` render option is **on by default** and gates personal-data qualifiers/reasons and secret reason text. Opt out with `complianceDetails: false`; other than namespace/global secret scope, models then keep byte-identical concept output without details.

Generated details require Chronicle 19.90.1 or later, which excludes compliance details text from the event-schema compatibility comparison ([Chronicle #4660](https://github.com/Cratis/Chronicle/issues/4660)). Changing details text no longer forces a new event generation; changing a secret scope still does.

## Option

| Entry point | Opt out of details |
| --- | --- |
| Default syntax renderer | `CratisRenderer.CreateDefault(complianceDetails: false)` |
| Syntax renderer with supplied collaborators | Set `ComplianceDetails = false` in the renderer's object initializer. |
| Direct concept rendering | `ConceptRenderer.Render(concept, applicationSet, rootNamespace, complianceDetails: false)` |

The parameterless factory, the default property value and the three-argument concept-rendering overload leave the option on. The option is an API choice, not a Screenplay declaration or a CLI flag.

The semantic artifact planner has no corresponding details option. Its ESM v13 protection mapping
above is unconditional; details never bypass protected identity admission.

## Attributes with the option enabled (default)

| Screenplay concept | Generated attributes |
| --- | --- |
| `pii` or `personal`, no reason or qualifiers | `[PII]` |
| `pii` with reason or qualifiers | `[PII]` and `[ComplianceDetails("<text>")]` |
| `secret`, no scope or reason | `[Encrypted]` and `[NotAudited]` |
| `secret` with scope or reason | `[Encrypted(EncryptionScope.<Scope>, "<reason>")]` and `[NotAudited]` |
| `pii secret` | Personal-data attributes only; secret scope and reason are ignored. |

Personal-data text joins these parts with one space, in order:

1. `GDPR Art. 9(1) special category: <human category>.`, if declared. Camel-case category names become lowercase words, such as `trade union membership`.
2. `GDPR Art. 10 criminal offence data.`, if declared.
3. The authored `pii reason`, unchanged.

Reasons are escaped as C# string literals, including quotes, backslashes, line breaks and Unicode line separators. These notes do not establish lawful processing, retention or a processing purpose.

`ComplianceDetailsAttribute(string details)` lives in `Cratis.Chronicle.Compliance`; `[PII]` remains in `Cratis.Chronicle.Compliance.GDPR`. In the pinned Chronicle client (19.110.1), `EncryptedAttribute` takes optional `EncryptionScope scope` and `string details`. Scope members are `Subject`, `Namespace` and `Global`. With details disabled, namespace/global secrets pass only the scope argument, using the constructor's default empty details string. With details enabled, a reason without a scope emits `EncryptionScope.Subject` explicitly because there is no reason-only positional overload; this preserves Chronicle's default. A scope without a reason emits an empty details string.

## Existing event generations

Mark concepts before first publish. Adding a marker to an already-published concept changes event
registration metadata and requires a new event generation and migration. Stage does not yet render
evolved events (`STAGE-ESM-026`), so re-rendering is not an in-place upgrade path.

Chronicle compares compliance and security metadata during event-type registration, including the metadata type that identifies encryption scope. Changing the scope of a concept used in persisted events requires a new event generation and migration: ciphertext has no scope or key id, so relabeling an existing generation can make old values unreadable or blank them on release.

Earlier Stage output with details disabled incorrectly used subject scope for namespace/global secrets. Re-rendering those concepts now corrects their scope. If events were already persisted using that output, introduce a new generation and migration rather than changing the stored generation in place.

Details text is excluded from Chronicle's compatibility comparison from 19.90.1, so adding or editing it does not require a new generation. Scope changes (`EncryptionScope`) are still compared and still require a new generation and migration. Use `complianceDetails: false` if you must target a Chronicle older than 19.90.1.
