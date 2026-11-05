---
title: Cratis renderer compatibility
description: Look up legacy Cratis renderer admission rules, diagnostics, key restrictions, and partial-output boundaries before choosing a rendering path.
---

Cratis Stage has a semantic artifact planner and a syntax-based compatibility renderer. Use
`CratisRendering.Plan` for a package-owned semantic plan; the legacy `IRenderer` writes directly to a target
directory and has the separate admission and failure behavior below. For current semantic capabilities, see
[How the Stage renders a model](stage-rendering.md).

## Semantic planning

`Cratis.Stage.Rendering.Cratis` exposes a pure `CratisArtifactRenderPlanner` for the executable semantic model
(ESM). It accepts an immutable `ArtifactRenderRequest` and returns the in-memory `ArtifactRenderPlan` before
anything is written. The plan carries an explicit artifact-schema version, and every artifact has a normalized
relative path, exact bytes, and SHA-256 hash. Typed error diagnostics make the plan non-publishable.

Use `CratisRendering.CreateProfile(...)` when constructing a lower-level request; do not reconstruct the profile
or choose target versions yourself. The planner requires the package-owned identities, versions, scaffold roster,
bytes, and hashes. A mismatch produces `STAGE-CRATIS-001`. The simpler `CratisRendering.Plan(...)` facade creates
that profile for you. See [Plan artifacts from Screenplay sources](plan-from-sources.md) for source loading,
selection, and publication checks.

Application, module, feature, and slice scopes produce the same paths and bytes for the artifacts they share.
Application scope also includes the fully resolved scaffold inputs. Unsupported reachable semantics block
publication by default rather than producing a thinner application; see
[Render around refused specifications](stage-rendering.md#render-around-refused-specifications) for the explicit
alternative and its limits.

Portable ESM collection queries render as Chronicle `Observe` subjects for observable lists and
`Task<IEnumerable<T>>` for snapshot lists, preserving `by` keys and equality filters. Generated React
routes wait for the first result; the Host still serves one-shot arrays. See
[Observable list queries](stage-rendering.md#generated-c-observable-list-queries) for SSE,
consistency and specification limits.

## Legacy event and projection boundaries

The published syntax-based `IRenderer` remains available through an explicit compatibility adapter. That legacy
path still renders its existing broader `ApplicationSyntax` surface and writes directly to a target directory.
Stage's syntax-based model conversion and legacy Cratis rendering reject marked or multiply declared events
with `STAGE-EVENT-001` naming the event rather than selecting an older shape. The semantic planner accepts only
executable models; it does not infer generations from syntax. For semantic projection capabilities and their
limits, consult the [rendering reference](stage-rendering.md) rather than assuming that legacy admission rules
also describe semantic planning.

## Legacy inline command handlers

Legacy inline command handlers support only C# (`csharp`) bodies proven not to bind to the generated `context`
parameter. Context-independent bodies such as `return Array.Empty<object>();` remain supported and are emitted
as authored, with the renderer's normal indentation. Screenplay and Arc have different command contexts; no
shared type-and-meaning mapping is assumed. Direct references, aliases, captures, `dynamic` aliases, and `nameof`
references to the parameter are rejected, including escaped and Unicode identifiers. Symbol-bound references to
other declarations, such as a shadowing lambda parameter or an anonymous object's `context` property, are not
references to the generated parameter.

Admission fails closed for non-C# languages, malformed or incomplete bodies, all preprocessor directives
(including inactive branches), analysis failures, and unresolved or ambiguous potential context bindings.
Unresolved unrelated generated event types do not by themselves block admission: this narrow binding check is
**not** a guarantee of arbitrary C# compilation, Screenplay-to-Arc semantic parity, or a security sandbox.
Declarative `produces` behavior is unchanged. File-backed command implementations have a separate guard below.

Every inline handler in a selected state-change slice is checked before emission, not only the first command;
`produces` is not a fallback for a rejected inline body. Rejection throws `UnsupportedInlineCommandHandler` with
code `STAGE-CRATIS-INLINE-001`, the command name, full slice path, exact authored code location, and reason.
The existing legacy failure flow skips that slice's artifact and specifications, continues independent output,
attempts an advisory failure marker, and ends with `RenderingFailed` rather than a success summary. Unselected
unsafe slices do not block a valid scoped render. The syntax compiler and ESM binder policies are unchanged.

## Legacy file-backed command implementations

Legacy file-backed command implementations are not supported. Every command in a selected state-change slice
is checked for `Handler.File` before inline admission or emission, including commands after the first. Rejection
throws `UnsupportedFileBackedCommandHandler` with code `STAGE-CRATIS-FILE-001`, the command name, full slice path,
exact command source location, and the symbolic file reference with its own source location. The fixed reason is
"file-backed command implementation not supported by legacy Cratis renderer", not a claim that the file is missing.
No implementation file is read or validated. File references remain valid Screenplay syntax and symbolic paths;
declaration file annotations are not handler implementations and do not trigger this guard.

This is a compatibility change: formerly ignored file implementations now block instead of silently emitting an
empty handler. Manually constructed syntax containing both a file and inline code or `produces` also blocks;
neither is a fallback. The DSL already rejects combining `handler` and `produces`. File rejection follows the same
existing partial-output failure flow described above. No supported external binding or replacement implementation
is supplied; migrate to declarative behavior only when it is equivalent. This narrow correction does not close
the broader Stage #13 file-backed implementation work or change other file implementation families.

## Legacy state-view query intent

Legacy state-view query rendering supports conventional unfiltered collections, identifying `by` lookups,
observable variants, and per-method authorization. It rejects declared filter parameter contracts and file or
inline performers rather than replacing their intent with an unrestricted query. Every query in a selected
state-view slice is checked before emission, including later queries and queries returning an unmatched model;
unselected slices do not block a scoped render. Direct `QueryRenderer` calls check only queries owned by the
requested read model, before changing the supplied builder. Rejection reports `UnsupportedQueryIntent` with code
`STAGE-CRATIS-QUERY-001`, the authored query and return-type names, typed reason, relevant source location, and
selected slice path when available. The location is the first filter declaration, otherwise the file or code
attachment; malformed performers use the performer declaration. Bodies are neither interpreted nor included in
the diagnostic, and file references are not opened. The affected slice and its specifications are skipped;
independent output continues, an advisory failure marker is attempted, and the operation ends with
`RenderingFailed`, not a success summary. The synthesized all/by-id pair still applies only when no declared
query returns the read model. Portable ESM query planning is unchanged.

## Legacy composite projection keys

Legacy state-view rendering also rejects **effective root `from` composite keys** in the first projection it
actually emits. After query admission and before slice emission, it checks the same subscriptions emission uses:
the first subscription for each event name wins, and an inline per-event key overrides the block key. A composite
block fully overridden by inline keys, or shadowed by earlier subscriptions, does not trigger this guard; an event
still falling back to that composite does. An unsupported explicit composite identity must not be confused with
an absent key, whose event-source-id default remains unchanged. Rejection throws
`UnsupportedCompositeProjectionKey` with code `STAGE-CRATIS-KEY-001`, selected slice path, authored projection and
read model names (the projection name when the read model name is omitted), event name, composite type name, and
original key source location without path normalization. No key-part values are evaluated.

This is a compatibility correction: previously the affected root subscription warned but emitted a bare
`FromEvent` attribute, losing the authored composite identity. The affected slice source and specifications are
now skipped; independent artifacts, including common types, may still be written, an advisory failure marker is
attempted, and rendering ends with `RenderingFailed`, not a success summary. An unsafe unselected slice does not
block a safe scoped render. This guard neither implements composite keys nor changes projection-level, nested,
children, join, parent, or unrendered-projection behavior. Root string literals have the separate bounded profile
below. The broader Stage #13 work remains open.

## Legacy root string keys

Legacy root string keys render as `[FromEvent<OrderCreated>(ConstantKey = "global")]`, using the public named
property, not a constructor argument or `[Key]`. This profile activates only when an **effective** root subscription
in the first rendered projection has a string literal key. It uses the same first-event winner and inline-key
override rules as emission; shadowed or overridden literals do not activate it. Once active, **every effective root
key** must be a nonempty string matching the anchored .NET expression `\A[\w ._/:\*\+\-]+\z`. This is the nonempty
counterpart of the payload grammar in Chronicle's pinned `ValueExpressionResolver`, not arbitrary-string support.
Unicode `\w`, spaces (including space-only values), and the listed punctuation survive unchanged; nothing is
trimmed. Empty strings, quotes, backslashes, parentheses, newlines, and expression-injection-like payloads are
rejected. Different admitted strings may coexist on different effective subscriptions.

The active profile rejects mixed default/property/nonstring keys, separate projection-level `key`, affected root
`parent` keys, and conventional identity on the inferred root record. Identity detection uses the actual inferred
C# properties, conservatively rejecting any ordinal-ignore-case spelling of `Id`, of any type, including properties
introduced by nested/children or other mappings. Generated properties have no serialization renames; this is not
general detection of custom serializers or arbitrary identifying attributes. The record's constructor, properties,
and schema remain unchanged: no synthetic `Id`, identity property, or `[Key]` is introduced.
The active profile also requires **at least one inferred root property** (`PropertylessRecord` rejection otherwise).
The guard records Chronicle 16.38.2's class-level `ConstantKey` behavior: it is applied during
constructor-parameter/property processing; a propertyless record retains the default event-source key.
That historical version is not a statement of Stage's current dependency pin. This bounded requirement does
not establish full materialization. Propertyless records in inactive/default profiles are unchanged.

Generated fixed by-id, declared single-instance, compatible `by`, and live by-id lookups use **`string` rather than
`Guid`**. This changes the generated public lookup signature and callers must adapt. Existing parameter names,
collection queries, and authorization are preserved. Every owned authored `by` must be primitive, required,
noncollection `String`; concepts, `Uuid`, other types, optional strings, and collections are rejected rather than
rewriting that explicit contract. Queries returning other read models do not affect this lookup profile, but the
existing all-selected-slice query-intent guard still runs first.

Unsupported active profiles throw `UnsupportedRootStringKey` (`STAGE-CRATIS-KEY-002`) with a typed reason,
selected slice, authored projection/read-model/event names, and the original relevant source location. Inferred
conventional identity uses the projection's location. The affected slice and its specifications are not returned;
independent output continues, the failure marker is attempted, and rendering ends with `RenderingFailed`.
The existing composite guard may reject a mixed composite profile first. This stricter blocking replaces earlier
lossy emission; it does not make other key families faithful. Inactive default/property profiles and nested, child,
join, removal, parent, and unrendered-projection behavior are otherwise unchanged.

Verification distinguishes three contracts: generated public attributes and unchanged record shape; actual generated
nonlive lookup calls preserving the complete string through `EventSourceId` into the public client's `ReadModelKey`;
and separate native Stage fixtures showing public attribute discovery/registration produces `$value(payload)` text.
These checks do not establish registration from the generated assembly, downstream resolver execution, persisted
materialization, or live-kernel behavior.

## Legacy partial output

Direct writes do not provide managed staging or safe stale-file removal: after a legacy rendering failure, treat
the target as **unsafe and incomplete** and use a fresh target. Safe staged publication remains owned by CLI #101.
