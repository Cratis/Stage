---
title: Concept compliance rendering
description: The opt-in complianceDetails option for personal-data notes and secret encryption scope in generated C# concepts.
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

The syntax renderer can carry Screenplay concept reasons, personal-data qualifiers and secret encryption scope into generated C# attributes. The `complianceDetails` render option is **off by default**: existing models keep byte-identical concept output, including bare `[PII]` and `[Encrypted]` attributes.

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

`ComplianceDetailsAttribute(string details)` lives in `Cratis.Chronicle.Compliance`; `[PII]` remains in `Cratis.Chronicle.Compliance.GDPR`. In the pinned Chronicle client (19.32.0), `EncryptedAttribute` takes optional `EncryptionScope scope` and `string details`. Scope members are `Subject`, `Namespace` and `Global`. A reason without a scope emits `EncryptionScope.Subject` explicitly because there is no reason-only positional overload; this preserves Chronicle's default. A scope without a reason emits an empty details string.

## Existing event generations

Chronicle compares compliance and security metadata during event-type registration. Enabling this option can change that metadata even when the payload shape is unchanged. Do not enable it for existing stored event generations without resolving the compatibility implications; changing encryption scope requires a new event generation.

The option should become the default once [Chronicle #4660](https://github.com/Cratis/Chronicle/issues/4660) settles details-only registration compatibility. Until then, leaving it off preserves the existing registration metadata.
