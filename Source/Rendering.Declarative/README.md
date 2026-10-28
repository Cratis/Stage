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
