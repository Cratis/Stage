---
title: Concept compliance metadata
description: How Cratis Stage preserves personal-data categories, criminal-offense classification, and operational-secret metadata in generated Chronicle attributes.
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Both the syntax renderer and the executable semantic model (ESM) renderer preserve concept
classifications as named arguments on Chronicle's `PIIAttribute`. Generated applications require a
Chronicle client exposing `PIIAttribute.SpecialCategory`, `PIIAttribute.Criminal`, and
`PIIAttribute.OperationalSecret`, plus the `Cratis.Chronicle.Compliance.GDPR.SpecialCategory` enum.

## Personal-data categories

The renderer maps the admitted Screenplay tokens exactly; it does not infer categories from reasons
or concept names:

| Screenplay special-category token | Chronicle enum member |
| --- | --- |
| `racialOrEthnicOrigin` | `SpecialCategory.RacialOrEthnicOrigin` |
| `politicalOpinions` | `SpecialCategory.PoliticalOpinions` |
| `religiousOrPhilosophicalBeliefs` | `SpecialCategory.ReligiousOrPhilosophicalBeliefs` |
| `tradeUnionMembership` | `SpecialCategory.TradeUnionMembership` |
| `genetic` | `SpecialCategory.Genetic` |
| `biometric` | `SpecialCategory.Biometric` |
| `health` | `SpecialCategory.Health` |
| `sexLifeOrSexualOrientation` | `SpecialCategory.SexLifeOrSexualOrientation` |

A criminal-offense qualifier adds `Criminal = true`. Both qualifiers are retained when declared
together, for example:

```csharp
[PII(SpecialCategory = SpecialCategory.Health, Criminal = true)]
```

Plain personal data still emits bare `[PII]`; absent qualifiers do not emit default-valued arguments.
Special-category and criminal-offense metadata classify personal data. They do not establish a lawful
basis for processing or independently change encryption, erasure, or auditing policy.

## Personal operational secrets

Personal data also marked `secret` retains one PII protection handler, its special-category and
criminal-offense qualifiers, and the operational-secret classification:

```csharp
[PII(SpecialCategory = SpecialCategory.Health, Criminal = true, OperationalSecret = true)]
[NotAudited]
```

Stage does not add `[Encrypted]` to that combination. `[NotAudited]` keeps the value out of Arc's
command causation chain; it does not encrypt event payloads. The concept stays in the application
rather than moving into an Arc-free public event contracts project. A public event requiring such a
concept produces the existing Arc-metadata contract diagnostic, just as a secret-only concept does.
Nondefault encryption scopes apply to secret-only concepts, not personal secrets.

Secret-only concepts retain their existing `[Encrypted]` and `[NotAudited]` mapping, including
namespace and global scope. Concepts without compliance metadata remain unmarked.

## Human-readable details

The syntax renderer's `complianceDetails: false` option omits human-readable `[ComplianceDetails]`
and secret reason text. It does not remove any named classification arguments, `[NotAudited]`, or
secret-only encryption scope. The semantic renderer emits classification arguments unconditionally
from `SemanticConcept.Compliance` and never emits `[ComplianceDetails]`.

See [Compliance rendering](compliance-rendering.md) for details, command-input exclusions, protected
identities, and the separate behavior of the Stage Host.
