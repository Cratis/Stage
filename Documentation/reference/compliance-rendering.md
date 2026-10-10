---
title: Concept compliance rendering
description: Secret encryption scope and the opt-in complianceDetails option for personal-data notes and secret reasons in generated C# concepts.
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

The syntax renderer always honors Screenplay secret encryption scope in generated C# attributes. Namespace and global secrets emit `[Encrypted(EncryptionScope.Namespace)]` and `[Encrypted(EncryptionScope.Global)]` even with details disabled. An unspecified or subject scope keeps the bare `[Encrypted]` output when details are disabled. Personal data remains `[PII]`, including `pii secret` concepts whose secret scope is ignored.

The `complianceDetails` render option is **off by default** and gates only personal-data qualifiers/reasons and secret reason text. Other than namespace/global secret scope, models keep byte-identical concept output with the option off.

## Option

| Entry point | Enable details |
| --- | --- |
| Default syntax renderer | `CratisRenderer.CreateDefault(complianceDetails: true)` |
| Syntax renderer with supplied collaborators | Set `ComplianceDetails = true` in the renderer's object initializer. |
| Direct concept rendering | `ConceptRenderer.Render(concept, applicationSet, rootNamespace, complianceDetails: true)` |

The existing factory and three-argument concept-rendering overload leave the option off. The option is an API choice, not a Screenplay declaration or a CLI flag.

The semantic artifact planner has no corresponding option: Screenplay refuses compliance-marked concepts at binding (`PLAY0268`), and semantic concepts do not carry this metadata. The option does not bypass that refusal or the syntax renderer's protected event-source identity refusal (`STAGE-CRATIS-COMPLIANCE-001`).

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

`ComplianceDetailsAttribute(string details)` lives in `Cratis.Chronicle.Compliance`; `[PII]` remains in `Cratis.Chronicle.Compliance.GDPR`. In the pinned Chronicle client (19.39.1), `EncryptedAttribute` takes optional `EncryptionScope scope` and `string details`. Scope members are `Subject`, `Namespace` and `Global`. With details disabled, namespace/global secrets pass only the scope argument, using the constructor's default empty details string. With details enabled, a reason without a scope emits `EncryptionScope.Subject` explicitly because there is no reason-only positional overload; this preserves Chronicle's default. A scope without a reason emits an empty details string.

## Existing event generations

Chronicle compares compliance and security metadata during event-type registration, including the metadata type that identifies encryption scope. Changing the scope of a concept used in persisted events requires a new event generation and migration: ciphertext has no scope or key id, so relabeling an existing generation can make old values unreadable or blank them on release.

Earlier Stage output with details disabled incorrectly used subject scope for namespace/global secrets. Re-rendering those concepts now corrects their scope. If events were already persisted using that output, introduce a new generation and migration rather than changing the stored generation in place.

Enabling details can also change registration metadata even when the payload shape is unchanged. The pinned Chronicle kernel rejects details-only changes to existing generations in production; add new generations and migrations before enabling the option for those events.

The option should become the default once [Chronicle #4660](https://github.com/Cratis/Chronicle/issues/4660) settles details-only registration compatibility. Until then, leaving it off omits details text, not encryption scope.
