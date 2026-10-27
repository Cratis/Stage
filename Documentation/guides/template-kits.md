---
title: Author a renderer kit
description: Use the stage-templates/v1 profile to prepare declarative templates, keep symbols structured, and handle safety refusals.
---

# Author a renderer kit

A renderer kit turns normalized model JSON into artifact parts. Use the `stage-templates/v1` profile when
several target packs need the same deterministic interpreter without sharing target-language spelling.
The profile is an interpreter building block; it does not itself install kits, resolve imports or write artifacts.

## Prepare once, evaluate in a plan

A loaded kit must require the `templates` contract at major version 1. Preparation scans, parses and lints
every declared output template, partial, path, condition and fan-out expression, even if its output is inactive.
It also validates declared operator tables and section overrides. All preparation diagnostics are returned
in deterministic code/path/line/message order. Any diagnostic means there is no usable `PreparedTemplates`.

Supply an `ITemplateLanguage` implementation when calling `TemplateProfile.Prepare`. Its `Pack` identifies
the target pack; its literal, type and placeholder methods own the target syntax. Throw `TemplateLanguageRefusal`
when the pack cannot safely express an input. The profile converts that exception into input refusal 024.
`Primitive`, `Collection` and `Optional` return `TypeSpelling` containing text and symbols, not flattened names.

Each `PreparedTemplates.BeginPlan(budget, types)` creates a disposable, non-thread-safe session with independent
state. `ITypeDirectory.TryGet` resolves semantic concept/composite identities to `TemplateSymbol` values.
Prepared templates may be shared between sessions; never share a session between concurrent evaluations.
Use `Render`, `RenderPath`, `EvaluateWhen` and `EvaluateForEach` for the declared sources. Path and expression
methods take an output ID; `Render` takes a declared template path. Missing conditions mean `true`; missing
fan-out means a single item containing the supplied scope. Present conditions must be Boolean and fan-out must
be an array of objects. Paths must remain text-only: a symbol or gap causes input refusal 024.

## Supply JSON scope

The scope has only `model`, `scope`, `item` and `args` roots. JSON objects and arrays are frozen recursively;
objects cannot introduce CLR values, delegates, type objects or ambient host members. Numbers are signed
64-bit integers or decimals. Duplicate object keys, private-use code points and excessive depth are refused.
Templates may declare locals and local functions, but cannot assign into the supplied scope or reserved helpers.

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

```scriban
{{ sym.decl 'Domain' 'Customer' }}
{{ sym.ref 'Other' 'Customer' }}
{{ sym.imports }}
```

These emit declaration, reference and imports insertion-point segments. The caller resolves qualification
and import policy after collecting parts. A handle may be assigned to a local, tested for truth or compared
to null, then emitted in the body. Capturing, stringifying, comparing with other values, slicing, placing it
in a container or reading its members causes symbol refusal 022. No placeholder sentinel text is used.

`type.ref` consumes `{kind, primitive?, target?, isCollection, isOptional}`. `kind` is `primitive`, `concept` or
`composite`. Primitive spelling belongs to the pack; concept/composite `target` is a semantic identity looked
up through `ITypeDirectory`. Collection wrapping precedes optional wrapping. Unknown identities cause 024.

## Delegate expression leaves

`expr.supported expression 'operators'` checks the normalized `$kind` expression tree.
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

See the generated [v1 helper reference](../reference/template-profile-v1.md) for exact signatures and the
closed standard-library allowlist. Changing a frozen signature, default, allowlist or vector requires an
explicit compatible contract/profile version decision; do not silently regenerate a released v1 freeze.
