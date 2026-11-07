---
title: Author a Cratis Stage renderer kit
description: Author a Cratis Stage renderer kit with the stage-templates/v1 interpreter, language-owned syntax, structured symbols, and plan-wide safety limits.
---

A Cratis Stage renderer kit uses templates to turn normalized model JSON into artifact parts. Choose the
`stage-templates/v1` profile when you want a shared interpreter while keeping target-language syntax in your
language pack. The interpreter evaluates templates and returns structured parts; it does not install kits,
resolve imports or write files.

Start by preparing a loaded kit with your language implementation. Then create a session for each plan,
supply its JSON scope and type directory, and collect the output parts. The caller resolves symbols and
imports before assembling artifacts. If you use `DeclarativeArtifactRenderPlanner`, that planner coordinates
these steps with its registered `IDeclarativeLanguage` implementation. For the surrounding profile, semantic
admission and publication boundaries, follow [Build a renderer target](build-renderer-target.md).

## Prepare once, evaluate in a plan

A loaded kit must require the `templates` contract at major version 1. Call `TemplateProfile.Prepare` with
that kit and an `ITemplateLanguage` implementation. Its `Pack` identifies the target pack, and its literal,
type and placeholder methods supply target syntax. `Primitive`, `Collection` and `Optional` return
`TypeSpelling` containing text and symbols, so later import resolution can still distinguish them. Throw
`TemplateLanguageRefusal` when the pack cannot safely express an input; the profile converts it into input
refusal 024.

Preparation scans, parses and lints every declared output template, partial, path, condition and fan-out
expression, including inactive outputs. It also validates operator tables and section overrides. Diagnostics
are returned in deterministic code/path/line/message order. Stop if any are returned: there is no usable
`PreparedTemplates` to evaluate.

For each plan, call `PreparedTemplates.BeginPlan(budget, types)` to create a disposable session with independent
state. `ITypeDirectory.TryGet` resolves semantic concept/composite identities to `TemplateSymbol` values.
Prepared templates may be shared between sessions; a session is not thread-safe and must never be shared
between concurrent evaluations.

Use `Render` with a declared template path. `RenderPath`, `EvaluateWhen` and `EvaluateForEach` instead take
an output ID. A missing condition means `true`; a present condition must return a Boolean. Fan-out means
repeating an output for each returned item: without it, evaluation returns one item containing the supplied
scope. A fan-out expression must return a JSON array. The declarative planner additionally requires each
item to be an object with a string semantic `id`, then orders items by that identity. Paths must remain
text-only: a symbol or gap causes input refusal 024.

## Supply JSON scope

Pass a JSON object with a `model` root and, when needed, an `item` root. The session accepts only those two
roots; extra scope roots are refused. JSON objects and arrays are frozen recursively, so templates can read
them without changing the input for later outputs. Objects cannot introduce CLR values, delegates, type
objects or ambient host members. Numbers are signed 64-bit integers or decimals. Duplicate object keys,
private-use code points and excessive depth are refused. Templates may declare locals and local functions,
but cannot assign into the supplied scope or reserved helpers.

Naming helpers consume nonempty arrays of nonempty words, never raw strings:

```scriban
{{ name.pascal model.nameWords }}
{{ name.snake model.nameWords }}
```

For `["order", "iD", "eMail"]`, these produce `OrderIDEMail` and `order_id_email`. Pascal and camel case preserve
the rest of each word. Other casing is invariant Unicode scalar casing, independent of host culture.
Literal helpers delegate escaping to the pack and never coerce strings into numbers or Booleans.
Use `lit['null']` for the zero-argument null literal: Scriban reserves `null` in dot-member syntax.

## Keep symbols structured

Keep declarations and references as symbol handles until the caller can choose qualification and imports.
This template declares one `Customer`, references another and marks where imports belong:

```scriban
{{ sym.decl 'Domain' 'Customer' }}
{{ sym.ref 'Other' 'Customer' }}
{{ sym.imports }}
```

Each helper emits a structured segment, not a prequalified name. After collecting the parts, the caller
resolves qualification and import policy. A handle may be assigned to a local, tested for truth or compared
to null, then emitted in the body. Capturing, stringifying, comparing with other values, slicing, placing it
in a container or reading its members causes symbol refusal 022. No placeholder sentinel text is used.

`impl.body <requirementId>` emits an authored-body handle with the same rules. The template never sees the body text: the caller looks the body up by requirement id and substitutes it byte for byte after the generated text is final.

`type.ref` consumes `{kind, primitive?, target?, isCollection, isOptional}`. `kind` is `primitive`, `concept` or
`composite`. Primitive spelling belongs to the pack; concept/composite `target` is a semantic identity looked
up through `ITypeDirectory`. Collection wrapping precedes optional wrapping. Unknown identities cause 024.

## Delegate expression leaves

Use expression helpers when a template needs to render a modeled condition without duplicating the whole
expression walker. `expr.supported expression 'operators'` checks the normalized `$kind` expression tree.
`expr.lower expression 'operators' 'leaf' 'throwing_operation_body'` lowers comparisons and logicals and delegates
resolved values, event context, read paths, refusals, arrays and composites to the declared `leaf` partial.
The partial receives the original leaf as `item` and may emit structured symbols. Scalar literal values use
the pack's literal methods. Table, leaf and role arguments must be literal names checked during preparation.

An operator table has exactly `open`, `close` and `operators` fields. `operators` contains `and`, `or`, `equal`,
`notEqual`, `greaterThan`, `greaterThanOrEqual`, `lessThan` and `lessThanOrEqual`. Each row has only `text` and
integer `precedence` from 1 through 100. Text and delimiters must be nonempty, single-line and free of private-use
code points. Unknown/duplicate keys, missing rows, out-of-range precedence and tables over 65,536 bytes cause 009.
All operators associate left. Lower-precedence children and equal-precedence right children are parenthesized.
An unsupported semantic node/operator produces a `STAGE-DEGRADE-001` gap and a pack-owned safe placeholder.

## Use partials and inline sections

Use a partial for shared template content and a section for a replaceable part of one template. Here,
`header` must be a declared partial; `body` supplies default text that a prepared override can replace:

```scriban
{{ render 'header' model }}
{{ wrap section 'body' }}
Default body for {{ model.name }}
{{ end }}
```

Only declared literal partial names may be rendered. Partials share the plan meter, symbol output and gaps.
Section names are unique per template, never nested, and match `[a-z][a-z0-9-]{0,63}`.
`PreparedTemplates.Sections` exposes them in source order. `TemplateSectionOverrides.Bodies` maps a
`(Template, Section)` tuple to a replacement body, parsed and linted at preparation. Unknown keys cause 010;
overrides containing sections cause 008. Overrides execute in the same scope and budget as the default body.

## Distinguish degradation from refusal

`gap role intent modelPath` records a typed gap and emits the pack's placeholder without refusing the plan.
The default code is `STAGE-DEGRADE-002`; explicit codes 003 and 005 are also allowed. Other codes cause 024.
Roles are `throwing_operation_body`, `deny_enclosing_operation`, `disabled_specification`, `unregistered_consumer`,
`retained_signature_or_type` and `graph_proven_omitted_leaf`. Intent must be one line of 1–512 characters;
model paths are at most 1,024 characters. Only the graph-proven omitted leaf may emit an empty placeholder.

`refuse 'trust' 'reason'` stops the entire plan. Allowed literal categories are `trust`, `integrity`, `budget`,
`authorization` and `event_stream`. Reasons are bounded to 512 characters. Safety refusals latch the session:
the current result has no usable output, `IsRefused` becomes true, and later calls return 041.
**The caller must discard all earlier artifacts from the plan**, not merely the failing artifact.

## Bound the worker process too

The default plan budget is 2,000,000 steps, recursion depth 64, fan-out 2,000, string length 262,144 UTF-16 units,
8,388,608 emitted UTF-8 bytes, 2,000 artifacts and a ten-second deadline. Limits aggregate across the plan,
including partials and output expressions. Source files are limited to 65,536 UTF-8 bytes and pre-scan nesting 32.
Budget refusals are 030–036. Preparation uses 001–010; ingress/host/symbol/input failures use 020–024;
explicit refusal is 040 and already-refused is 041, all prefixed `STAGE-TPL-`.

In-process counters are defense in depth, not a secure sandbox. Scriban builtins can allocate or spend time
between meter callbacks. Run untrusted kits in a separately bounded process with memory/CPU/time limits;
on a worker timeout, kill it, refuse the whole plan and discard its artifacts. A mutable Scriban context alone
is not a security boundary. The profile exposes no file loader, clock, environment, regex, dynamic evaluation
or unrestricted CLR access, but does not provide process isolation.

When you are ready to write the templates, use the generated [v1 helper reference](../reference/template-helpers.md)
for exact signatures and the closed standard-library allowlist. Changing a frozen signature, default,
allowlist or vector requires an explicit compatible contract/profile version decision; do not silently
regenerate a released v1 freeze. Return to [Build a renderer target](build-renderer-target.md#verify-the-generated-code)
to verify the assembled artifacts against the target toolchain: successful template evaluation alone does
not prove that the generated program compiles or preserves the modeled behavior.
