<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Stage declarative planner

`Cratis.Stage.Rendering.Declarative` is the prerelease, unpackaged render planner that interprets a `stage-target/v1` kit through the `stage-templates/v1` profile over the render model. No host wires it in yet, and it takes no dependency on any language pack.

`DeclarativeArtifactRenderPlanner` implements `IArtifactRenderPlanner`. Its profile names the renderer `stage-declarative` at `1.0.0-prerelease` and carries exactly one input, named `kit`: the canonical kit archive. The plan refuses, with zero artifacts and one error, when the renderer differs, the roster is not exactly the kit, the kit fails to load or its digest differs from the recorded one, the profile target differs from the manifest target, a kit language pack is bound to no registered language or to more than one, or the templates fail to prepare. Every `STAGE-DECL-*` code is a safety refusal.

A successful admission records the kit, language pack, template profile, helper catalogue and template engine as input components on the plan's `InputProvenance`.

## Language seam

A language pack implements `IDeclarativeLanguage`: the template language and type directory contracts, plus `Resolve` (declarations, references and the imports block of one file become ordered generated or authored pieces, or an integrity refusal) and `Admit` (a verdict on one authored body that the planner never rewrites). Segments are handles, never token text.

## Output expansion

Outputs expand in manifest order. An output without `forEach` renders once with the scope `{ model }`. With `forEach`, items are ordered ordinally by their string `id` and each renders with `{ model, item }`; `when` decides inclusion. An item without a string `id`, or any template refusal, refuses the whole plan.

## Rendering, symbols and gaps

Each expanded output renders its path and its template. The template segments become `DeclarativeSegment` handles and the language `Resolve`s them into pieces; text is never searched or patched. A language refusal (for example an ambiguous short name) refuses the whole plan with the language's own code. Generated text that contains sentinel-shaped residue (`/*__stage_`) refuses the plan with `STAGE-DECL-009`.

A template gap is not a refusal. The gap belongs to the fan-out item whose output reported it, or, for a single output, to the construct its `modelPath` names. The planner emits the language placeholder, a `STAGE-DEGRADE-*` warning owned by that construct and its degradation evidence. A gap that names no construct is recorded as plan evidence owned by the application. Gap roles map one to one onto placeholder kinds.

## Implementation bodies

A template places an authored body with the `impl.body` helper. The planner resolves the requirement, checks that its language matches the pack and that the SHA-256 (UTF-8, lowercase hex) of the supplied content equals the recorded content hash, and asks the language to admit it. A hash mismatch is an integrity refusal. A missing body or an unsupported one is a gap: the language placeholder plus a `STAGE-DEGRADE-003` warning on the requirement's owner.

Generated text is normalized to LF per generated run. Authored bodies are carried byte for byte. A marker that straddles a generated|authored boundary is not detected, because authored bytes may legitimately contain anything.

## Paths and omitted constructs

Every rendered path must be a portable relative path (`STAGE-DECL-011`) and unique ignoring case (`STAGE-DECL-012`); either refuses the plan.

Every selected command and reaction is accounted for in the plan's inventory. A construct that no output touches is `Degraded` with one `STAGE-DEGRADE-002` warning ("The kit renders no output for ..."): a command gets the `DenyEnclosingOperation` placeholder kind, a reaction `UnregisteredConsumer`. Such a plan scores below 100% and the other outputs still render.

## Refusals and gaps

| Situation | Result |
| --- | --- |
| Safety (every `STAGE-DECL-*` code, template refusals, language refusals) | Plan refusal: zero artifacts and an error |
| A template gap, a missing or unsupported body, an omitted construct | Warning, placeholder, degraded owner; artifacts still render |

## Codes

| Code | Meaning |
| --- | --- |
| `STAGE-DECL-001` | The renderer or its version is not the declarative planner |
| `STAGE-DECL-002` | The inputs are not exactly one input named `kit` |
| `STAGE-DECL-003` | The kit digest differs from the recorded one |
| `STAGE-DECL-004` | The profile target differs from the kit target |
| `STAGE-DECL-005` | A language pack is bound to no registered language, or to more than one |
| `STAGE-DECL-006` | The kit binds more than one language pack |
| `STAGE-DECL-007` | The render model could not be built |
| `STAGE-DECL-008` | A fan-out item has no string `id` that is a valid semantic id |
| `STAGE-DECL-009` | Generated text contains sentinel-shaped residue |
| `STAGE-DECL-010` | An implementation body does not match its content hash |
| `STAGE-DECL-011` | A rendered path is rooted, traverses or is not portable |
| `STAGE-DECL-012` | Two rendered paths are equal ignoring case |
| `STAGE-DECL-013` | The assembled text of a file cannot be a text artifact (for example a leading byte order mark) |

## Scope

The planner is curated-only: the roster is one kit input, and only registered languages are used. It is prerelease and not packable yet; packaging is pending #800.

## Coverage accounting and output limits

Coverage counts a construct only through a per-construct `forEach` item or a gap it owns. A slice-level output does not mark the commands and reactions it contains as rendered, so those constructs are reported Degraded.

The output budget is enforced twice: the template session meters template output (including authored bodies), and the planner caps the aggregate final text of all files by the same `MaxOutputBytes`, because language resolution (for example import blocks) adds text the session never sees. Exceeding either refuses with `STAGE-TPL-031` and no artifacts.
