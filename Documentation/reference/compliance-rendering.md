---
title: Concept compliance rendering
description: Concept protection in ESM v13 generated applications, protected identity refusals, and opt-in syntax-renderer compliance details.
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Stage renders compliance markers on a concept's generated `ConceptAs` record. In generated event
payloads, Chronicle applies these markers to scalar concepts, collection elements, and concept-valued
members inside composite records. Personal data uses encryption and erasure; operational secrets use
encryption without erasure.

## Executable semantic models

Stage admits exact ESM v1–v14 language/semantic pairs from Screenplay 4.146.0. The semantic planner maps
`SemanticConcept.Compliance` using fully qualified attribute names:

| Concept classification | Generated attributes |
| --- | --- |
| `pii` or `personal` | `[global::Cratis.Chronicle.Compliance.GDPR.PIIAttribute]` |
| `secret`, unspecified or subject scope | `[global::Cratis.Chronicle.ProtectedValues.EncryptedAttribute]` + `[global::Cratis.Arc.Chronicle.Commands.NotAuditedAttribute]` |
| `secret scope namespace` or `global` | `[global::Cratis.Chronicle.ProtectedValues.EncryptedAttribute(global::Cratis.Chronicle.ProtectedValues.EncryptionScope.Namespace)]` (or `.Global`) + `[global::Cratis.Arc.Chronicle.Commands.NotAuditedAttribute]` |
| `pii secret` | `[PII]` only; personal classification takes precedence. |

The semantic path never emits `[ComplianceDetails]`. GDPR special-category and criminal qualifiers
are carried by ESM v13 but do not change the generated attribute. Reasons are authoring metadata,
not ESM members. Opt-in details remain a syntax-renderer feature ([Stage #259](https://github.com/Cratis/Stage/issues/259)).
Unmarked models retain their earlier ESM version and byte-identical artifacts.

On the semantic path, each command input that transitively contains any compliance-marked concept
(`pii`, `secret`, or both) also gets
`[property: global::Cratis.Arc.Chronicle.Commands.NotAuditedAttribute]`. This uniform rule includes
scalar inputs, optional inputs, collections, and nested composites. Arc omits the entire input from
causation, including its unprotected members; unrelated inputs remain auditable. The concept-level
attributes remain in place for event-payload protection.

Arc 22.68.0 already withholds scalar `[PII]` concept inputs through Chronicle's `PIIMetadataProvider`.
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

**Play/the Stage Host does not encrypt or redact values of personal-data concepts.** Its own event
store keeps values in plain text; it does not apply the generated application's compliance attributes.
Operational secrets are not protected there either. Use synthetic data for Play. Host protection is
tracked in [Stage #346](https://github.com/Cratis/Stage/issues/346). Passing reference or Stage
specifications proves modeled outcomes, not storage protection.

## Syntax renderer details

The syntax renderer always honors Screenplay secret encryption scope in generated C# attributes. Namespace and global secrets emit `[Encrypted(EncryptionScope.Namespace)]` and `[Encrypted(EncryptionScope.Global)]` even with details disabled. An unspecified or subject scope keeps the bare `[Encrypted]` output when details are disabled. Personal data remains `[PII]`, including `pii secret` concepts whose secret scope is ignored.

The `complianceDetails` render option is **off by default** and gates only personal-data qualifiers/reasons and secret reason text. Other than namespace/global secret scope, models keep byte-identical concept output with the option off.

## Option

| Entry point | Enable details |
| --- | --- |
| Default syntax renderer | `CratisRenderer.CreateDefault(complianceDetails: true)` |
| Syntax renderer with supplied collaborators | Set `ComplianceDetails = true` in the renderer's object initializer. |
| Direct concept rendering | `ConceptRenderer.Render(concept, applicationSet, rootNamespace, complianceDetails: true)` |

The existing factory and three-argument concept-rendering overload leave the option off. The option is an API choice, not a Screenplay declaration or a CLI flag.

The semantic artifact planner has no corresponding details option. Its ESM v13 protection mapping
above is unconditional; details never bypass protected identity admission.

## Attributes with the option enabled

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

`ComplianceDetailsAttribute(string details)` lives in `Cratis.Chronicle.Compliance`; `[PII]` remains in `Cratis.Chronicle.Compliance.GDPR`. In the pinned Chronicle client (19.45.0), `EncryptedAttribute` takes optional `EncryptionScope scope` and `string details`. Scope members are `Subject`, `Namespace` and `Global`. With details disabled, namespace/global secrets pass only the scope argument, using the constructor's default empty details string. With details enabled, a reason without a scope emits `EncryptionScope.Subject` explicitly because there is no reason-only positional overload; this preserves Chronicle's default. A scope without a reason emits an empty details string.

## Existing event generations

Mark concepts before first publish. Adding a marker to an already-published concept changes event
registration metadata and requires a new event generation and migration. Stage does not yet render
evolved events (`STAGE-ESM-026`), so re-rendering is not an in-place upgrade path.

Chronicle compares compliance and security metadata during event-type registration, including the metadata type that identifies encryption scope. Changing the scope of a concept used in persisted events requires a new event generation and migration: ciphertext has no scope or key id, so relabeling an existing generation can make old values unreadable or blank them on release.

Earlier Stage output with details disabled incorrectly used subject scope for namespace/global secrets. Re-rendering those concepts now corrects their scope. If events were already persisted using that output, introduce a new generation and migration rather than changing the stored generation in place.

Enabling details can also change registration metadata even when the payload shape is unchanged. The pinned Chronicle kernel rejects details-only changes to existing generations in production; add new generations and migrations before enabling the option for those events.

The option should become the default once [Chronicle #4660](https://github.com/Cratis/Chronicle/issues/4660) settles details-only registration compatibility. Until then, leaving it off omits details text, not encryption scope.
