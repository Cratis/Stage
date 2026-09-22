---
title: Default generated Scene
description: Eligibility, input formats, result selection and proxy registration for the default Cratis application screen.
---

The Cratis application artifact planner uses an authored Scene unchanged when the profile carries one.
Otherwise it emits `scene.json` with command forms and eligible editable keyed lookups, plus `src/bindings.ts`.
It does not mutate the profile or generate per-screen TypeScript components.

## Lookup eligibility

Backend semantic admission runs before composition. A default lookup requires all of the following:

- A modeled keyed query with optional-single (`ZeroOrOne`) snapshot semantics, not a collection.
- A unique exact semantic query name across the application.
- A scalar text or UUID argument, either primitive or a non-enumerated concept over that primitive.
  These generate native Arc `String` or `Guid` parameter descriptors.
- At least one required, non-identifier, non-key **own** read-model property whose native value is a string,
  number or boolean. Text, whole number, decimal and boolean primitives and non-enumerated concepts qualify.

Eligible result properties are ordered by stable semantic property identity, ordinally; the first is selected.
The name is the actual generated member name, including Arc's leading-acronym convention, not a property path.
The canonical `ProjectById` lookup displays `name`, never its Guid-valued `projectId`.

Unsupported lookups are omitted, not replaced by inert or permanently failing components. Numeric, boolean,
date, composite and enum keys do not get an input form. Guid/date/object/collection/nullable result properties
are not chosen. A model with only a key or no supported result retains its command forms but has no lookup.
No query is invented for a model without one; if no commands or eligible lookups remain, no default Scene is
composed. Malformed, optional or collection identifiers are rejected by the semantic model before composition.

## Emitted component properties

The canonical external component uses `Cratis.Components:queryInputForm` with these properties:

```json
{
  "query": "ProjectById",
  "inputs": [{
    "parameter": "projectId",
    "type": "string",
    "label": "project id (GUID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)",
    "required": true,
    "pattern": "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}(?![\\s\\S])"
  }],
  "resultField": "name",
  "label": "Find project summary",
  "submitLabel": "Search"
}
```

The argument name comes from the same naming convention as the generated C# query parameter. The native
proxy generator emits `new ParameterDescriptor('projectId', Guid, false)` for the canonical model and
`String` for the text-key variant. Both use `type: "string"`; Scene passes entered strings unchanged.

Guid inputs explicitly require the dashed 8-4-4-4-12 hexadecimal format before HTTP, accepting either case.
No version, variant, nonzero-ID or backend authorization rule is added. Text inputs have **no pattern**,
trimming, coercion, defaults or generated IDs. Required inputs reject empty/whitespace-only drafts; other
text is exact, including surrounding whitespace. Submission, not editing, commits query arguments.

## Binding identity

Stage imports each query's actual proxy export from its declaring slice folder, with a distinct local alias,
and emits one `registerQueryIdentity(name, sourceIdentity, proxy)` call per source. `sourceIdentity` is the
query's stable semantic ID (`sem1:…`), not its entered key. No source is collapsed by name and no duplicate
legacy `registerQueries` call is emitted. Query element IDs use `query:` followed by that semantic ID, so
query and command elements cannot collide even when their display names match.

Scene's identity registry resolves a unique identity for existing authored tables and `singleResult` elements.
The legacy host registration fallback remains Scene-owned. Different sources with the same query name remain
ambiguous: both are registered, but neither gets a default lookup form. The current textual ESM v1 binder also
rejects duplicate query names; registration still preserves distinct identities in supplied semantic models.
Command registration and command form behavior are unchanged. Bindings and Scene JSON contain no HTTP route.

## Package and verification boundary

The emitted frontend pins the Scene family at exact **3.6.0** for this contract. Components **4.9.0**,
Fundamentals **7.19.3**, Arc **22.16.1** and the runtime image **18.2.0** remain unchanged. Stage's own Scene
NuGet tooling pins are separate and do not need to move for an external-component payload.

Stage tests parse the emitted JSON and build the generated Guid/text backends with the native Arc proxy
generator. Changes to this profile also require installing the exact published frontend packages, typechecking
and bundling the generated application, and verifying the native consumer contract. C# rendering tests alone
do not establish those integration gates or browser acceptance.
