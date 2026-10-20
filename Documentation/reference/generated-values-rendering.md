---
title: Generated values and command responses
description: Generation strategies, freshness guards, deterministic fixtures and command responses in rendered applications.
---

A create command can return its new identifier without accepting that identifier from the caller. Stage renders generated concepts as Arc Handle parameters and carries scalar or named responses back to the caller. ESM v15 adds strategy-specific factories and atomic freshness guards; v20 admits rules on generated concepts. The generated backend takes its exact Arc pin from Stage's `Directory.Packages.props`.

This page is the terse reference. To follow one model from `.play` to rendered C# and its runtime behavior, read [Generate identifiers and values from a model](../guides/generated-values.md).

## Model to C# mapping

This state-change slice generates an event-source identifier and returns it. It declares no strategy, so the model stays below ESM v15:

```screenplay title="project.play"
concept ProjectId : Uuid
module Storefront
  feature Projects
    slice StateChange Register
      command RegisterProject
        projectId ProjectId generated identifier
        name String
        produces event ProjectRegistered
          name String = name
        returns projectId
      specification RegisteringWithAPin
        when RegisterProject
          for "11111111-1111-1111-1111-111111111111"
          name = "Apollo"
        then ProjectRegistered
          name = "Apollo"
        then returns "11111111-1111-1111-1111-111111111111"
```

Stage renders the identifier concept and the command with its event. These are excerpts: license headers, usings and doc comments are omitted.

```csharp title="Common/ProjectId.cs (excerpt)"
public record ProjectId(global::System.Guid Value) : global::Cratis.Chronicle.Events.EventSourceId<global::System.Guid>(Value), global::Cratis.Concepts.IGeneratable<ProjectId>
{
    public static readonly ProjectId NotSet = new(global::System.Guid.Empty);

    public static ProjectId New() => new(global::Cratis.Concepts.GenerateValue.Uuid());

    public static implicit operator ProjectId(global::System.Guid value) => new(value);
}
```

```csharp title="Storefront/Projects/Register/Register.cs (excerpt)"
[global::Cratis.Arc.Commands.ModelBound.CommandAttribute]
[global::Cratis.Arc.Authorization.AuthorizeAttribute]
public record RegisterProject(string Name)
{
    public (global::Acme.Shop.Common.ProjectId, global::Cratis.Chronicle.EventSequences.EventForEventSourceId) Handle([global::Cratis.Arc.Commands.ModelBound.GeneratedValueAttribute("projectId")] global::Acme.Shop.Common.ProjectId projectId)
    {
        return (projectId, new global::Cratis.Chronicle.EventSequences.EventForEventSourceId(projectId, new global::Acme.Shop.Storefront.Projects.Register.ProjectRegistered(Name)));
    }
}
```

The generated property is absent from the command constructor and request inputs. Its modeled name becomes the case-sensitive generated-value name. Mappings read the Handle parameter. An event targeting that identifier uses an explicit `EventForEventSourceId`; `GetEventSourceId()` never references a removed request property.

A generated concept that is not an identifier derives from `ConceptAs<T>`. It implements `IGeneratable<T>` with `New()` for non-time-ordered strategies, or `IGeneratableInContext<T>` with `New(GenerationContext context)` for time-ordered strategies. The example above uses implicit UUID generation and stays below ESM v15; its handler remains unguarded and returns one `EventForEventSourceId`.

## Version admission

Stage admits exact ESM v1–v30 language/semantic pairs. Version admission does not imply support for every strategy:

| Version | Generation support |
| --- | --- |
| v15 | UUID, UUID-v7, ULID, TypeID, NanoID and bytes factories |
| v20 | Rules on generated concepts; unpinned rendered specifications require every rule to be proven |
| v21 | Pattern and inclusive Int64 random generation; checksum validation; verified C# `generate rule` bodies render through `IGeneratableInContext<T>`; executor runs pinned String rule values without invoking the body; Host refuses commands generating rule values |

The Host samples pattern and random values through the Fundamentals release pinned in Stage's `Directory.Packages.props`. The independent specification executor requires deterministic fixtures, just like Screenplay 4.182.0. Both paths enforce the reference strategy shapes and checksum rules. For `generate rule`, the executor accepts any String fixture that satisfies the concept's rules and never invokes the implementation body; missing pins remain `Unsupported(IdentityAllocation)`. An unused rule-generation concept does not block the Host or executor. The Host refuses each command that generates a rule value with `STAGE-ESM-028`, naming the command, property, concept and rule (or the requirement id when the requirements are unavailable). This is by design: a `generate rule` always has an implementation body that Stage's interpreter never runs or replaces, so render the application to run it. The supported generation subset is the built-in strategies (`uuid`, `uuidv7`, `ulid`, `typeid`, `nanoid`, `bytes`, `pattern` (optionally with a checksum), and `random`): the Host generates them and checks them against the concept's rules, while the executor checks pinned fixtures for them. The renderer compile-checks and emits verified C# rule bodies; the legacy syntax engine still refuses `generate rule` with `STAGE-ESM-028`. The legacy syntax renderer still refuses checksum and unknown validation rule kinds with `STAGE-ESM-042` rather than omitting the check. No artifacts are published for a refused semantic render plan.

### Rule generation

A String concept's `generate rule` body renders as a private static String method, called by `New(global::Cratis.Concepts.GenerationContext context)` through `IGeneratableInContext<T>`. Arc supplies the command receipt time as `context.Now` and the operation's tenant as `context.Tenant`. Bodies may use BCL APIs such as `Guid.NewGuid()` and Fundamentals APIs such as `GenerateValue.Ulid(context)`; the latter binds to the Cratis C# target's context type.

Pass the implementation requirements, resolved contents and typed-context descriptors from the same Screenplay compilation to the renderer; ESM bytes alone do not carry the factory body. Stage verifies the attachment's content hash and typed-context envelope, parses one complete C# statement block, and compile-checks it against embedded BCL and Fundamentals metadata references. The pinned Fundamentals DLL is a build-only input embedded in the renderer, not a runtime or package dependency. It does not run the body while rendering or apply the reducer/policy purity gate. Compilation errors and warnings report `STAGE-ESM-019` at the body location, because the generated application treats warnings as errors; namespace or type shadowing reports `STAGE-ESM-022`. Missing or unresolved attachments fail `STAGE-ESM-020`; rendering does not replace them with stubs.

Any concept rule makes rule generation Unproven, so rendered specifications need a pin (`STAGE-ESM-038`). A rule-generated concept with no validation rules can generate unpinned through the context factory. Verified pure C# Boolean concept rule bodies render as app-local typed-context validators, including checks on pinned generated values. Verified pure C# concept code-validation iterators also render, adding one error per yielded message. Iterator interpolations use invariant formatting, not the host culture; reads of validator context `Tenant`, `CausedBy` or `Occurred` fail with `STAGE-ESM-015`. The reference and executor still require pins, and the Host interpreter still refuses commands that generate rule values.

### Checksum rules

Declare `validate checksum luhn`, `validate checksum mod11` or `validate checksum mod97` on a String concept. The rendered validator calls `global::Cratis.Concepts.GenerateValue.HasValidChecksum(value, global::Cratis.Concepts.Checksum.Luhn)` (or `Mod11` / `Mod97`), retaining the authored message and severity.

The final one digit (Luhn/Mod11) or two digits (Mod97) must exactly match the checksum of the preceding ASCII digits. Printable non-digits in the payload, including lowercase letters and spaces, are ignored; they are not converted to numbers. Control characters and non-ASCII characters anywhere in the value fail validation. Empty values and non-digit suffixes fail too. Mod11 payloads whose check digit would be 10 have no valid one-digit suffix. For example, `79927398713` passes Luhn, `abc98` passes Mod97, and `79927398714` fails Luhn. A checksum is not a length or format constraint; declare those separately.

The specification executor and generated-fixture admission check concept rules through Screenplay's public `SemanticValidationRules.Check` API. Generated C# continues to use Fundamentals for runtime checks.

When a concept declares checksum validation, pinned generated fixtures must satisfy it as well as the generation shape. A bad pin blocks rendering with `STAGE-ESM-011`; it is not treated as invalid caller input.

### Pattern and random strategies

| Strategy | Backing type | Rendered factory |
| --- | --- | --- |
| `pattern "REF-?{16}"` | String | `GenerateValue.Pattern("REF-?{16}")` through `IGeneratable<T>.New()` |
| `pattern "{yyyy}-{MM}-{dd}-?{16}"` | String | `GenerateValue.Pattern(pattern, context)` through `IGeneratableInContext<T>.New(context)` |
| `pattern "#{16}" checksum luhn` | String | `GenerateValue.Pattern(pattern, Checksum.Luhn)`; `Mod11` and `Mod97` use the corresponding enum values |
| `random a..b` | Int | `GenerateValue.Integer(a, b)` through `IGeneratable<T>.New()`; both Int64 endpoints are inclusive |

Pattern factories sample distinct strings uniformly from the finite language. Dated patterns expand every date segment from the operation's captured UTC date, not the render-time clock. The date-free factory does not require a context. Random values remain numeric throughout Host execution and generated specifications. ESM v21 whole-number concepts render as `long`, so the compiler's entire Int64 random range is representable without an Int32 range refusal.

A pattern fixture checks language membership, including trailing digit width for a generation checksum. It does not check checksum arithmetic unless you also declare `validate checksum`. Without an authored clock, date positions in a pin need only have the correct digit widths; with a clock, all repeated date segments must match that exact UTC date. A random fixture must be an integral number inside the inclusive range. Neither strategy guarantees uniqueness: generated destinations retain the existing freshness guard, and constrained payloads retain append-time collision handling.

## ESM v15 strategies

Declare a strategy on the concept, for example `generate uuidv7` under a Uuid concept or `generate bytes 16 hex case upper` under a String concept. The command property still says `generated`. To pick one, see [Choosing a strategy](../guides/generated-values.md#choosing-a-strategy).

| Strategy | Backing type | Factory and default shape |
| --- | --- | --- |
| `uuid` | Uuid or String | `GenerateValue.Uuid()`; version 4, RFC variant, lowercase hyphenated text |
| `uuidv7` | Uuid or String | `GenerateValue.UuidV7(context)`; version 7, RFC variant |
| `ulid` | String | `GenerateValue.Ulid(context)`; 26 uppercase Crockford symbols |
| `typeid "user"` | String | `GenerateValue.TypeId("user", context)`; lowercase prefix, underscore and a UUID-v7 Crockford suffix; an empty prefix omits the underscore |
| `nanoid` | String | `GenerateValue.NanoId(size, alphabet)`; default size 21 and URL-safe alphabet; custom size 1–256 |
| `bytes n base64url` | String | `GenerateValue.Bytes(n, ValueEncoding.Base64Url)`; URL-safe text without padding |
| `bytes n hex` | String | `GenerateValue.Bytes(n, ValueEncoding.Hex)`; lowercase hexadecimal |
| `bytes n base32` | String | `GenerateValue.Bytes(n, ValueEncoding.Base32)`; uppercase base32 without padding |

String-backed strategies accept the modifiers admitted by Screenplay: `prefix` adds printable ASCII text before the generated portion, and `case upper` or `case lower` selects its casing without changing the prefix. Bytes accept counts 1–512; unused trailing encoding bits must be zero. Screenplay refuses unsupported strategies and identifiers below 64 random bits before rendering. It still refuses compliance markers (`pii`, `secret`) on a generated concept with `PLAY0268`, so a secret cannot be generated yet. Validation rules are admitted from ESM v20, subject to the limits below. See [Keep the secret out of the identity](../guides/generated-values.md#keep-the-secret-out-of-the-identity).

Time-ordered concepts implement `IGeneratableInContext<T>` instead of `IGeneratable<T>`. Arc supplies a `GenerationContext` whose `Now` is the command's `ReceivedAt`, and its tenant. `Ulid`, `TypeId` and `UuidV7` read only `Now`. An event that maps `$context.occurred` takes the same receipt time, truncated to a millisecond, for its payload and its `Occurred` metadata, so the value's time component and that mapping agree to the millisecond. An event that does not map it carries no occurrence time from Arc, and Chronicle stamps the append time. Hand-written calls to these concepts must now pass a context to `New(context)`; the parameterless `New()` is no longer emitted. `uuid`, `nanoid` and `bytes` retain `New()`.

### Rules on generated concepts

At ESM v20, Stage renders a generated concept's factory together with its `ConceptValidator<T>`. Arc validates the factory output or pinned value before calling `Handle`. A failing rule is a server generation defect: the command result carries an exception, no input `ValidationResults`, and no appended events. The Host's evaluator checks the generated output's strategy shape and rules only after authorization and ordinary input checks. A generated-fixture contract rejection then becomes a server exception, including for reaction-invoked commands; denial and input validation keep their ordinary results.

The reference and independent specification executor report `Rejected(Contract)` with `Generated fixture '<property>' fails concept '<Concept>' rules`, not the rule's authored validation message. They check each supplied fixture's shape and then rules in fixture order, before reporting missing fixtures. This represents the same generation defect, not a caller-correctable validation failure.

Rendered specifications may leave a generated property unpinned when every concept rule is proven. Stage derives each rule's proof status with Screenplay's `RuleConsistency.Check` rather than reading it from ESM. Any unproven or refuted rule requires a pin (`STAGE-ESM-038`); this check applies to explicit strategies and implicit UUID generation. A pin failing its shape or rules fails `STAGE-ESM-011`; Stage does not render it as an input-validation assertion or an exception assertion. The command and concept can still render without that specification. Opaque or unsupported rules are refused before any pin is evaluated: `STAGE-ESM-005` identifies the pinned specification, generated concept and rule rather than throwing during planning.

Warning- and information-severity rules on generated concepts render alongside error-severity rules. Arc rejects generated values failing a rule of any severity as server generation defects. Rule severity is separate from Screenplay's proof diagnostics: `PLAY0688` refuses a refuted rule and `PLAY0689` warns that a rule is unproven.

### Fresh event sources

In an application with at least one explicit concept generation strategy, a command that produces facts on a generated destination returns `EventsWithConcurrencyScopes`, with one `ConcurrencyScope(EventSequenceNumber.BeforeFirst, EventSourceId: id)` per generated source. Chronicle checks every scope in a batch once, against the state before the batch, so several initial facts in the same invocation are allowed. One violated scope rejects the entire batch and appends nothing; Arc reports it as `ValidationResultReason.ConcurrencyViolation`. When a storage append loses a sequence-number race, Chronicle renumbers the batch and validates the scopes again before retrying. It does so from Chronicle 19.40.1 (verified in the 19.55.2 kernel). See [Chronicle concurrency](/chronicle/events/concurrency/). Generated rejection specifications assert that reason rather than Arc's message text.

The Host and specification executor use the reference freshness message: "A generated event source already has facts; retry the command with a fresh generated value." Earlier facts without a destination never make the generated source stale. A generated value that is not the destination, such as an invite code appended to a customer stream, adds a scope only when its text equals the destination's, so it makes no freshness claim about that stream. An application using only implicit UUID generation remains unguarded, including at v16–v21; the schema version alone does not activate freshness. Generated handle parameters declare `Freshness = GeneratedValueFreshness.Unguarded` so that Arc's own guard, which is `Guarded` by default from Arc 22.100, does not apply on top of these scopes. The explicit scopes will move to Arc's generated-identifier freshness contract ([Arc #3129](https://github.com/Cratis/Arc/issues/3129)).

### Unique generated payloads

The shop's `InviteCode` can be a short NanoID without becoming the event-source identifier:

```screenplay
concept InviteCode : String
  generate nanoid 6 alphabet "AB"
module Shop
  feature Invitations
    slice StateChange ReleaseInvite
      event InviteReleased
      command ReleaseInviteCode
        inviteId String identifier
        produces InviteReleased
          for inviteId
    slice StateChange Invite
      constraint UniqueInviteCode
        unique code on Invited
        released by InviteReleased
        message "Invite code is already in use"
      event Invited
        code InviteCode
      command InviteCustomer
        inviteId String identifier
        inviteCode InviteCode generated
        produces Invited
          for inviteId
          code = inviteCode
      specification Collision
        given Invited
          for "A"
          code = "AAAAAA"
        when InviteCustomer
          inviteId = "B"
          generated inviteCode = "AAAAAA"
        then error "Invite code is already in use"
```

Chronicle checks uniqueness at append, not with a preflight query. If another event source holds the generated code, the command is rejected with `ValidationResultReason.ConstraintViolation`, the authored constraint name and its message; no event is appended. The caller retries the rejected command and gets a fresh generated value. Stage does not automatically retry, regenerate or rerun the handler, and a new draw is not guaranteed to succeed in a small or exhausted code space. Retry only a known rejection, not an unknown commit outcome.

This is current-claim uniqueness, not all-time uniqueness. The same source may reclaim its own code, replacing a claim frees the previous code, and `InviteReleased` frees the owner's claim for reuse. `ignore casing` compares codes without case, so distinct generated spellings can still collide. High entropy lowers collision probability; it does not prove uniqueness. A returned candidate on a failed command is not a committed code.

Generated constraints retain the ordinary admission limits: one required scalar text property, event-sequence scope, and only one affected claim or release event per command batch. String-backed UUID, UUID-v7, ULID, TypeID, NanoID (sizes 1–256) and bytes (counts 1–512) can supply that property. UUID-backed payloads remain refused under `STAGE-ESM-014`. Short non-identifier codes are admitted; Screenplay's 64-bit minimum for generated identifiers still applies. Only ordinary constrained request inputs receive the not-null validator; generated values remain Handle parameters.

## Conditional productions and shared slices

A StateChange slice can declare several commands. Each command keeps its own validator, response and specifications, while sharing the slice's event contracts and source file.

Portable `produces when` conditions read command inputs and generated values. Stage evaluates each condition once and returns a typed collection of `EventForEventSourceId` values containing only the taken productions. When the application uses explicit concept generation, freshness scopes cover only sources targeted by those productions. If every production is skipped, the command is accepted, appends nothing and still returns its response, even if the generated source already has facts.

Conditions support text and Boolean equality, numeric equality and ordering, and `and`/`or`. Enum operands and expression-valued `Condition` nodes remain refused with `STAGE-ESM-006`; production route overrides on an unkeyed stream under a keyed command route, or with the literal stream id `Default`, are not admitted yet and are refused with `STAGE-ESM-036`. A specification that reads a generated property in a production condition must pin it, so the expected production sequence can be determined before rendering.

Commands without production conditions retain their existing generated Handle shape.

## Responses

| Screenplay contract | C# Handle response |
| --- | --- |
| `returns projectId` | The property's type, paired with the event result |
| `returns` block | A `<Command>Response` record, paired with the event result |
| Response-only command | The scalar or response record directly, without an event tuple |

A response block names each field independently of its source property. Each field keeps the source type, including optionality. Generated parameters and ordinary command properties can supply response values, including composite `type` values; a composite response renders as the app-local record and serializes as the same JSON object. Collection responses are refused with `STAGE-ESM-038`. Arc uses the tuple's response element as `CommandResult<T>.Response`; it does not expose the event wrapper as the response.

The generated TypeScript proxy carries the response type too. A UUID-backed scalar response uses `Command<IRegisterProject, Guid>`; a named response uses `Command<IRegisterReceipt, RegisterReceiptResponse>`. The event wrapper is not the client response type.

When `<Command>Response` would collide with another generated type in the application, such as a command named `IssueInvoiceResponse` beside `IssueInvoice`, or with one of the record's own fields, Stage renders the record as `<Command>Response_` and reports `STAGE-ESM-050`. Field names are never renamed, so the response JSON stays the same. Response fields that collide with each other or with a record member such as `EqualityContract` are refused with `STAGE-ESM-038`.

A generated concept named `New` keeps its name: its factory implements `IGeneratable<New>.New` explicitly instead of declaring a public `New` member, which C# forbids on a type of the same name.

## Fixtures and testing

A specification's `when ... for <uuid>` pins a generated identifier. `generated receipt = "<uuid>"` pins a generated non-identifier property. Stage emits `PinGeneratedValue` before executing the scenario, using the concept type rather than a raw `Guid`.

For the model above, the generated setup contains this line, taken from `when_registering_with_apin.cs`:

```csharp
_scenario.PinGeneratedValue("projectId", new global::Acme.Shop.Common.ProjectId(global::System.Guid.Parse("11111111-1111-1111-1111-111111111111")));
```

Pinned event and `then returns` assertions compare the authored values. A named return expectation can assert a subset of the response fields.

When a rendered specification omits a fixture, Stage calls `_scenario.AllowGeneration()`. Ordered event destinations and payload mappings that depend on that unpinned property compare against `_scenario.GeneratedValues.Last<T>(name)`, and response assertions compare against the same capture. Below v15, the UUID must be nonempty. At v15, every unpinned capture must also match its strategy's derived encoding shape. These are relational assertions, not assertions that random generation equals an authored UUID literal. Unordered or projected expectations require pins. A generated value involved in an asserted constraint collision also requires a deterministic pin. Stage requires a pin when prior claims on other sources could make an unpinned draw change success into rejection; unrelated unpinned relational specifications still render.

For an unpinned time-ordered value, Stage emits `_scenario.Clock(new DateTimeOffset(2026, 1, 2, 3, 4, 5, 678, TimeSpan.Zero))` as the first setup statement, before policy registration and givens. A `should_generate_<name>_at_the_receipt_time` fact checks the capture's encoded millisecond prefix as well as its shape. The random portion remains unpinned. If a command specification states `given clock`, Stage uses that authored instant instead, normalized to UTC, for both the scenario clock and the receipt-time prefix assertion. Authored clocks also produce clock setup for fully pinned commands and commands without time-ordered generation. Without an authored clock, those specifications get no clock setup. Clocks must be millisecond-aligned and at or after the Unix epoch; `when clock`, query-only, append and reaction clocks remain refused.

Dated pattern captures use the same deterministic scenario clock as time-ordered values and assert the expanded UTC date in their shape. Random captures assert the inclusive range. Date-pattern rule proofs use the authored UTC year, or the fixed scenario year 2026 when no clock is authored, keeping rendered bytes deterministic.

The in-memory specification executor requires deterministic fixtures, just like the Screenplay evaluator. Missing fixtures produce an `Unsupported(IdentityAllocation)` outcome; they do not silently allocate a value and pass. It compares scalar responses and each asserted named field after successful execution.

Rendering is not compilation or execution. Build the generated project in Debug, then run its Debug specifications. A successful run proves that Arc resolves the parameters, the events land on the intended streams, and the response assertions execute.

## Partial-perform runtime

The live semantic runtime supplies a value from the concept's strategy for every generated property, including validation-only requests: a semantic number for `random`, and semantic text for the other strategies. Applications without explicit concept generation retain implicit UUID generation, regardless of their schema version. It rejects attempts to supply generated properties as request inputs. Validation-only requests do not append facts or reserve the generated UUID for a later execution.

For time-ordered strategies, the Host uses the same occurrence instant for generation and event metadata. It takes that instant from `CommandContext.ReceivedAt`, retaining a UTC wall-clock fallback only for hand-built contexts without a receipt. Reaction-invoked Host commands use the triggering command's occurrence; rendered reactions dispatch through Arc and use their own pipeline receipt.

An accepted scalar response becomes the HTTP command result's `response` value. A named response becomes an object with camelCase field names. Rejected or unauthorized results carry no response. The Stage frontend passes the accepted response through Scene's `CommandOutcome.response`.

Form `on submit` response binding and navigation remain blocked on [Scene #53](https://github.com/Cratis/Scene/issues/53). Generated TypeScript proxies already expose the scalar or named command response; they do not supply Scene's continuation binding.

## Limits and diagnostics

Generated property names may match renderer locals such as `operation`, `occurred`, `stageEvents`, `stageProduces0`, `streamIdPart0` or `source`. The Cratis renderer renames its locals with the first free numeric suffix; generated-value parameter names and their bindings stay unchanged. These names do not restrict Host or specification-executor admission.

Commands with generated properties can route events to the generated identifier within a named stream. Stream ids must come from request values or literals, never generated properties: Screenplay resolves the route before generation. When concept generation is enabled, generated destination freshness spans all streams of that event source, even when a source definition declares narrower concurrency dimensions. Bare `generate uuid` is normalized to legacy implicit UUID generation and does not enable this guard on its own; use an explicit strategy such as `generate uuidv7` when requiring freshness.

Routed commands can return a scalar or named response, including the generated identifier and request values. Conditional productions return the response even when no event is selected. Generated TypeScript proxies expose the response type, not the routing or event wrappers.

Route-format failures append no events, but do not promise effect-free generation. In a rendered app, route checks stay in `Handle` and return a `ValidationResult` after Arc resolves generated parameters; the reference evaluator rejects the route as `Contract` before generation. Deterministic pins that fail generation shape or concept rules are already refused at render time with `STAGE-ESM-011`.

Commands with generated properties can validate and require request values. Requirements run before generation: an unmet requirement rejects the request without drawing a generated value or appending events. Rules on generated concepts are checked during generation; pin those values in specifications when their rules are unproven. Command rules and requirements cannot read a generated property because it does not exist during request validation; Screenplay rejects those reads, and Stage refuses hand-built models containing them with `STAGE-ESM-038`.

| Diagnostic | Refused shape |
| --- | --- |
| `PLAY0268` (Screenplay) | Compliance markers on a generated concept |
| `STAGE-ESM-028` | Host (by design): each command generating a rule value, not unused rule concepts; the rule body is never run by the interpreter. The semantic renderer emits verified C# rule bodies; the executor admits pinned String rule values. Unknown generation strategies, non-concept generated values, optional values and collections remain refused, and the diagnostic names the offending shape (optional, collection, primitive or composite type) and the Screenplay contract that a generated property must be a required, non-collection generatable concept (`PLAY0483`); the legacy syntax engine refuses every explicit strategy, including `rule`, and generated properties whose concepts have rules |
| `STAGE-ESM-042` | Unsupported validation rule kinds in the legacy syntax renderer, including checksum; semantic checksum rules are supported |
| `STAGE-ESM-011` | A generated fixture that fails its required strategy shape or concept rules |
| `STAGE-ESM-004` | StateChange slices without a command |
| `STAGE-ESM-006` | Expression conditions, enum operands and nonnumeric ordering in production conditions |
| `STAGE-ESM-038` | Command rules or requirements that read a generated property; collection responses, responses from a source that is not a command property of the same type, or response fields whose C# members collide; the message names the reason |
| `STAGE-ESM-038` | Unpinned generation on a concept with any unproven rule, read by a production condition, or with unordered event, read-model or query expectations, or a generated constrained value whose unpinned draw could change the asserted collision outcome |
| `STAGE-ESM-038` | A generated identifier combined with a destination-less plain production |

The last case is not shorthand for using the generated identifier. Screenplay keeps legacy allocation in a separate `AllocatedIdentities` channel. Use an inline `produces event` declaration as above, or an explicit production destination. Stage refuses the ambiguous combination instead of sending the event to a different stream than the evaluator.

Time-ordered factories (`uuidv7`, `ulid`, `typeid`) follow Arc's `ReceivedAt`, with no wall-clock fallback in the rendered app. Missing receipt times fail generation. Pins bypass the factory and need not encode the receipt time. Values reveal the receipt time and are not monotonic within a millisecond. Screenplay `given clock` is supported on otherwise admitted command specifications from ESM v6. Fixed `$context.occurred` payload assertions use that instant; a wrong expected instant fails rather than changing the clock. The executor also uses the authored instant for stored occurrence metadata, while retaining deterministic generated fixtures. Authored clocks with reactions, `when clock` and `sequence` remain unsupported. The executor can run pinned `rule` values without executing bodies. The Host interpreter refuses commands generating them; rendered applications execute the verified bodies through the context factory.

## Pitfalls

- Do not put a generated identifier back into the request to make a spec deterministic. Pin it in the scenario instead.
- Do not edit generated commands or concepts to select a strategy. Declare it on the concept in the model.
- Adding an explicit strategy anywhere in the application, or rules on the generated UUID concept itself, requires its fixture to be a lowercase version-4 UUID with the RFC variant. A pin such as `11111111-1111-1111-1111-111111111111` no longer matches; uppercase Uuid text is noncanonical even before the strategy check.
- A pin has the exact concept type and modeled name. A raw UUID or differently cased name is not the same fixture.
- A returned UUID is not proof that a projection is ready, an external effect succeeded, or an identifier is absent from storage.

For the complete target admission table, see [Build a renderer target](../guides/build-renderer-target.md).
