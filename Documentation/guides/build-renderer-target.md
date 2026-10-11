---
title: Build a renderer target
description: Implement a deterministic, target-neutral Screenplay-to-code planner with the Stage artifact contracts.
---

A renderer target turns a compiled Screenplay executable semantic model into a complete, reviewable set of target artifacts. Build one when Screenplay models should produce code for another framework or platform without adding target-specific semantics to Screenplay or Stage's shared contracts.

```mermaid
flowchart LR
    Play[Screenplay files] --> Compile[SemanticModelCompiler]
    Compile --> ESM[Executable semantic model]
    ESM --> Execute[SemanticExecutionPlan]
    Execute --> Admit[Target admission]
    Admit --> Plan[IArtifactRenderPlanner]
    Plan --> Artifacts[ArtifactRenderPlan]
    Artifacts --> Publish[Managed publisher]
```

Rendering is the opposite direction from source recovery. A source adapter such as `IDotNetScreenplayAdapter` recovers source into Screenplay; an `IArtifactRenderPlanner` realizes Screenplay as target code. Do not interpret source-adapter facts as renderer inputs.

## Use the public contracts

Reference:

- `Cratis.Screenplay` for `ExecutableSemanticModel`, semantic identities, and `SemanticExecutionPlan`;
- `Cratis.Stage.Contracts` for `ArtifactRenderRequest`, profiles, scopes, planned artifacts, and diagnostics;
- target framework packages only from the target integration and generated-code verification projects.

The renderer extension surface is the contracts under `Cratis.Stage.Contracts.Rendering`. The Cratis renderer is a useful behavioral example, but its admission classes, semantic indexes, naming code, emitters, scaffold conventions, and other internal helpers are Cratis implementation details. They are not reusable renderer APIs. Build target-owned equivalents around the public contracts rather than taking a dependency on `Cratis.Stage.Rendering.Cratis`.

## Compile Screenplay before calling the planner

Compilation belongs to the caller or orchestration layer, not the target planner. Compile every file in one logical application through `SemanticModelCompiler`. Stable document keys establish identity; display paths provide diagnostic context and must not establish identity.

```csharp
using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

var applicationName = "Projects";
var catalog = SemanticIdentityCatalog.Empty(
    ApplicationIdentity.Create(applicationName));
var documents = new[]
{
    (StableKey: "concepts", Path: "Concepts.play", Text: File.ReadAllText("Concepts.play")),
    (StableKey: "registration", Path: "Projects/Registration.play", Text: File.ReadAllText("Projects/Registration.play"))
}.Select(source => SemanticSourceDocument.Create(
    catalog.ResolveDocument(source.StableKey),
    source.StableKey,
    source.Path,
    source.Text)).ToImmutableArray();

var documentSet = SemanticDocumentSet.Create(documents, catalog);
var compilation = new SemanticModelCompiler().Compile(
    applicationName,
    documentSet);
if (!compilation.Success)
{
    foreach (var diagnostic in compilation.Diagnostics)
    {
        Console.Error.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
    }

    return;
}

var model = compilation.Value!.Model;
var execution = SemanticExecutionPlan.Compile(model);
if (!execution.Success)
{
    foreach (var issue in execution.Issues)
    {
        Console.Error.WriteLine($"{issue.Kind}: {issue.Details}");
    }

    return;
}

var executionPlan = execution.Plan!;
```

Do not create an `ArtifactRenderRequest` until both phases succeed. The execution plan is a capability-admitted view of the same model, and `ArtifactRenderPlan.Create(...)` rejects a request when `request.ExecutionPlan.Revision` differs from `request.Model.Revision`.

The portable execution plan deliberately supports a narrower executable subset than the complete semantic model. A target must honor execution-plan issues and must not reparse Screenplay syntax or names to bypass them. If a target needs semantics that are not present, keep that behavior unsupported until Screenplay exposes an additive executable contract.

## Keep diagnostic ownership clear

Each phase owns its failures:

1. `SemanticModelCompiler` owns source, parse, binding, and semantic diagnostics in `compilation.Diagnostics`.
2. `SemanticExecutionPlan.Compile(...)` owns portable execution-capability issues in `execution.Issues`.
3. The target planner owns profile-admission, target-capability, and target-emission diagnostics in `ArtifactRenderPlan.Diagnostics`.
4. The publisher owns destination safety, ownership, recovery, and write failures.

Do not copy compiler diagnostics or execution issues into target diagnostic codes. Orchestration can map all three diagnostic types into one presentation model, but each phase's native code or issue kind, details, severity when present, and location or semantic identity remain authoritative.

Use `ArtifactRenderDiagnostic` for an unsupported but valid request. Use stable, target-owned codes and attach the affected `SemanticId`; use the default identity only for a genuinely application-wide diagnostic.

```csharp
static ArtifactRenderDiagnostic Unsupported(
    string message,
    SemanticId artifact) => new(
        "ACME-RENDER-002",
        ArtifactRenderDiagnosticSeverity.Error,
        message,
        artifact);
```

An `Error` makes `ArtifactRenderPlan.Success` false. `Information` and `Warning` do not. Invalid contract construction, such as a malformed scope, a traversing artifact path, or colliding output paths, throws `InvalidArtifactRenderContract`; those are programmer or integration errors rather than unsupported target semantics.

## Define and admit an exact target profile

`ArtifactRenderProfile` records all realization choices that can change output. At minimum, version:

- the stable target identity and exact target profile version;
- the stable renderer identity and exact renderer version;
- the target framework or package generation;
- projection, persistence, concept, and optional transport conventions;
- every scaffold, template, configuration, or schema input that affects artifacts.

Resolve input bytes before planning. Do not derive versions or templates from ambient packages, the destination, or the machine running the renderer.

```csharp
using System.Collections.Immutable;
using System.Text;
using Cratis.Stage.Contracts.Rendering;

var projectInput = ArtifactRenderInput.Create(
    "project-file",
    "1",
    ImmutableArray.CreateRange(Encoding.UTF8.GetBytes(
        "{\n  \"name\": \"generated-application\"\n}\n")));

var profile = ArtifactRenderProfile.Create(
    target: "acme",
    targetVersion: "3.2.0",
    renderer: "acme-code",
    rendererVersion: "1.0.0",
    inputs: [projectInput]);
```

`ArtifactRenderInput.Create(...)` validates the name and version, stores immutable bytes, and computes a lowercase SHA-256 hash. `ArtifactRenderProfile.Create(...)` rejects duplicate name-and-version pairs and orders inputs by ordinal name and version.

The planner must still admit the profile. Match all four identity/version fields and an exact input roster. Reject missing, extra, renamed, or unexpectedly versioned inputs with a target diagnostic. Do not use `Single()` or `SingleOrDefault()` on unadmitted input so a wrong profile cannot turn into an incidental exception.

```csharp
static bool Supports(ArtifactRenderProfile profile)
{
    if (!string.Equals(profile.Target, Target, StringComparison.Ordinal) ||
        !string.Equals(profile.TargetVersion, TargetVersion, StringComparison.Ordinal) ||
        !string.Equals(profile.Renderer, Renderer, StringComparison.Ordinal) ||
        !string.Equals(profile.RendererVersion, RendererVersion, StringComparison.Ordinal) ||
        profile.Inputs.Length != 1)
    {
        return false;
    }

    var input = profile.Inputs[0];
    return string.Equals(input.Name, "project-file", StringComparison.Ordinal) &&
        string.Equals(input.Version, "1", StringComparison.Ordinal);
}
```

Accepted input bytes must be parsed or copied deterministically wherever they affect output. `ArtifactRenderPlan` carries target and renderer versions but does not repeat input provenance, so the target's profile contract and support matrix must document the accepted roster.

### Localized Cratis validation messages

Screenplay retains `$strings.<key>` in the ESM as a **key**, not display text. If a model uses these messages on command or concept validation rules or command `require` guards, pass its companion `.strings` file contents to the additive `CratisRendering.CreateProfile(applicationName, options, scene, stringsFiles, defaultLocale)` overload. `stringsFiles` maps relative `<base>.<locale>.strings` paths (using `/`, not `\`) to original file contents; choose an explicit `defaultLocale` present in that set. Locale tags are checked syntactically without consulting the planner host's installed culture data. The generated application must run with culture data (such as ICU) that supports the selected locales; it resolves them at startup. The caller reads files **before** planning; the planner only consumes validated, hashed profile bytes. Without the optional catalog, `STAGE-ESM-002`/`STAGE-ESM-005` still reject these messages.

```csharp
var profile = CratisRendering.CreateProfile(
    model.Application.Name,
    new CratisRenderingOptions("Invoices", "Invoices"),
    scene: null,
    stringsFiles: new Dictionary<string, string>
    {
        ["invoicing.en.strings"] = "invoices.reasonRequired = \"Reason is required\"\n",
        ["invoicing.nb.strings"] = "invoices.reasonRequired = \"Begrunnelse kreves\"\n"
    },
    defaultLocale: "en");
var request = new ArtifactRenderRequest(
    model,
    executionPlan,
    profile,
    new ArtifactRenderScope(ArtifactRenderScopeKind.Application, model.Application.Id));
var plan = new CratisArtifactRenderPlanner().Plan(request);
```

The profile validates file names and assignments with Screenplay's `StringsFile.Parse`, merges disjoint keys by locale, and rejects duplicate keys and locale tags that differ only by case rather than depending on discovery order. It emits `GeneratedStrings.cs` from the catalog contents. With a catalog, generated `Program.cs` enables ASP.NET Core request localization before Arc, so the request's `Accept-Language` UI culture reaches validators while the formatting culture remains invariant. Generated validators resolve text at **validation time** using `CultureInfo.CurrentUICulture`: first the requested locale, then its parent locales, then the declared default. A missing translation in a non-default locale falls back; a referenced key absent from the default locale blocks planning with `STAGE-ESM-018` and **zero artifacts**. Referenced values containing braces (FluentValidation message placeholders) fail with `STAGE-ESM-018` instead of silently changing the text. Control characters and surrogate code units in **any** catalog value cause `CreateProfile` to throw `InvalidCratisBackendApplicationScaffold`, even if no rule references the key; this also excludes emoji represented by surrogate pairs. Line separators (U+2028 and U+2029) and other non-printable characters that the catalog accepts are escaped in generated C# string literals. Arc's validation-result `Message` contains localized text; `State` retains the original `$strings.<key>`. Generated `then error "$strings.<key>"` specifications assert `State`, matching the reference evaluator's key comparison rather than pinning a process locale. Literal and generated-default messages remain unchanged. This is a backend validation-message realization, not a general-purpose strings resolver for screen labels or constraint errors.

## Implement a pure planner

`IArtifactRenderPlanner.Plan(...)` is a pure planning boundary. Given the same immutable request, it must return the same plan or the same contract failure. It must not:

- read or write files;
- start a compiler, formatter, package manager, or other process;
- use the network;
- read the clock, random values, environment variables, current directory, or machine identity;
- inspect installed or workspace dependencies;
- mutate the semantic model, execution plan, profile, or profile input bytes.

```csharp
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Acme.Screenplay.Rendering;

public sealed class AcmeArtifactRenderPlanner : IArtifactRenderPlanner
{
    public const string Target = "acme";
    public const string TargetVersion = "3.2.0";
    public const string Renderer = "acme-code";
    public const string RendererVersion = "1.0.0";

    public ArtifactRenderPlan Plan(ArtifactRenderRequest request)
    {
        var diagnostics = new List<ArtifactRenderDiagnostic>();
        var artifacts = new List<PlannedArtifact>();

        if (!Supports(request.Profile))
        {
            diagnostics.Add(new(
                "ACME-RENDER-001",
                ArtifactRenderDiagnosticSeverity.Error,
                "The request does not match the supported target, renderer, versions, and input roster.",
                request.Model.Application.Id));

            return ArtifactRenderPlan.Create(request, [], [.. diagnostics]);
        }

        var selected = SelectScope(request);
        diagnostics.AddRange(Admit(request, selected));
        if (diagnostics.Any(_ => _.Severity == ArtifactRenderDiagnosticSeverity.Error))
        {
            return ArtifactRenderPlan.Create(request, [], [.. diagnostics]);
        }

        artifacts.AddRange(Emit(request, selected));
        if (request.Scope.Kind == ArtifactRenderScopeKind.Application)
        {
            artifacts.Add(PlannedArtifact.CreateBinary(
                "project.json",
                request.Profile.Inputs[0].Bytes));
        }

        return ArtifactRenderPlan.Create(
            request,
            [.. artifacts],
            [.. diagnostics]);
    }

    // Supports, SelectScope, Admit, and Emit are deterministic target-owned code.
}
```

The sample shows the sequencing, not reusable helper APIs. `SelectScope`, `Admit`, and `Emit` are target-owned functions that you implement against the public semantic and rendering contracts.

## Admit target semantics before emission

Create one target-owned admission phase. Evaluate the selected artifacts and all semantics reachable from them before emitting dependent files. Admission must decide whether the target can exactly realize:

- each reachable slice kind;
- command validation and authorization;
- produced-event destinations, conditions, and property mappings;
- projection transitions and affected-instance cardinality;
- query cardinality, delivery, keys, and authorization;
- concepts, composite types, optionality, and validation;
- executable specification values and expectations;
- target lifecycle, persistence, transport, and package choices.

Collect independent target diagnostics so an author can fix several issues in one pass. When a required semantic fails admission, do not emit its dependent artifacts. Never substitute thinner code, an unfinished implementation, an empty handler, a placeholder value, or a guessed default.

The Cratis renderer applies the same rule when emission reaches a semantic kind or value combination it has no rendering for, such as an unhandled slice kind, type-reference kind, or primitive, or a semantic value whose kind does not match its primitive type. Planning stops with a `STAGE-ESM-012` error attached to the application identity. The plan is unsuccessful and contains no artifacts, including scaffold files. The renderer does not fall back to `object` or `default!`, and it does not drop the slice. Admission also raises `STAGE-ESM-012` for concept, event, command, composite-type, and read-model members whose generated PascalCase names collide with another property, the enclosing record name, record-synthesized or inherited members, or generated command/query methods. Record members named `Clone` are forbidden by C#; `Finalize` is allowed as a positional property. Enum values may share their enclosing enum's name, but two values that normalize to the same name are rejected. Query method overloads such as `ToString(IReadModels, …)` are allowed unless they collide with a property, the record name, or a non-overloadable member. That diagnostic is attached to the offending record; a projection referencing an event in any child or nested scope receives it on the projection, even when the event belongs to another slice. These admissions also stop the plan without artifacts. The admission checks prevent uncompilable output; an emission-time `STAGE-ESM-012` reports a renderer gap. Neither adds executable semantic model capability, and Cratis admission remains narrower than the executable semantic model.

A model that uses an ESM v2 construct arrives as language and semantic version `2.0`. Admit it construct by construct rather than treating it as v1. The Cratis renderer handles the v2 constructs this way:

- **Event-source routes (ESM v8).** Named sources and streams render as Chronicle definitions; commands select them through an Arc attribute and set keyed stream identities inside the handler, after authorization and validation. See [Event-source routing](../reference/event-source-routing.md) for generated shapes, the identity codec and reserved names. Command specifications seed routed givens and assert explicit routes or `unrouted`; routed read-model/query replay still fails `STAGE-ESM-030`.
- **Typed destination.** A command appends to the event source named by `produces … for <property>`, and the identity is not copied into the event record. The rendered command provides that property as its event source id. When a produced event has no destination of its own, the command's typed destination applies, as it does in the Screenplay evaluator.
- **Specification event sources.** When a `given`, `when`, or `then` event states `for <value>`, the rendered specification seeds the given fact or asserts the appended event on that stream rather than on the command's destination value. In a specification that expects success, the stated sources must agree with each other, have the command destination's type, convert to a Chronicle event source id without losing identity (text or UUID), and have an expected event to assert them on. Otherwise the specification fails admission with `STAGE-ESM-011`. A rejection appends nothing, so a `when … for` on a rejection specification is admitted as is.
- **Command occurrence values.** A `$context.occurred` mapping renders when the target property has the occurrence type. The generated handler obtains Arc's dispatch receipt time through an `IOperationContextAccessor` parameter, truncates it to UTC milliseconds (Chronicle's storage precision), and uses it both for the payload property and for `EventForEventSourceId.Occurred` on every event the command produces. This is the same receipt that opaque policies see, not a later reading of the clock. A missing or unset receipt throws `CommandReceiptTimeUnavailable` before any event is returned; there is no wall-clock fallback. A specification that asserts a fixed value derived from that occurrence fails admission with `STAGE-ESM-011`, because the generated handler uses the actual time. `$context.identity.*` and other caller mappings still fail admission with `STAGE-ESM-013`.
- **Scoped projections.** Supported `from`, `join`, `every`, `nested from`, `children`, and keyed root/child removal blocks render as fluent Chronicle projections. `STAGE-ESM-017` rejects conflicting event roles or keys, and nested `from` without a same-contract root `from` and identical key: Chronicle can otherwise create a partial root document when the reference evaluator would not. Child removal through a join renders at the child level, including removal from several parents. Nested `clear` and subsequent re-creation render when the clear event also has a same-contract root `from` and identical key. Chronicle 19.32.0 differential and MongoDB checks cover both shapes. Root join removal remains blocked by [Chronicle #4263](https://github.com/Cratis/Chronicle/issues/4263) and [Screenplay #563](https://github.com/Cratis/Screenplay/issues/563); joins or children inside nested blocks remain blocked by Chronicle #4125. `all` (`FromAll`) fails admission until Chronicle #4266 is fixed, because the in-memory and MongoDB results differ for unrelated event sources. Composite keys fail admission until Chronicle #4265 provides a lookup by composite key. Literal mappings render in `from` blocks and in `every` blocks at levels without joins, only for literals Chronicle's fluent value expression carries exactly (`^[\w ._/:*+-]*$` for text). Each `every` literal lowers to a Set mapping on each `from` builder at its own root, child, or nested level, replacing an exact same-target Set so the literal wins. Ancestor/descendant target overlaps and collisions with non-Set from mappings fail `STAGE-ESM-017`. A level with joins and an `every` literal also fails `STAGE-ESM-017` until [Chronicle #4663](https://github.com/Cratis/Chronicle/issues/4663) supplies native `FromEvery` constants; rewriting from mappings can otherwise change join backfill. Literals in `all` remain refused under Chronicle #4266. `STAGE-ESM-017` also rejects event-property names Chronicle reads as expressions (`$`, `.`, `[`, `]`, a leading quote, booleans, numbers, or derived functions such as `Week` or `Week()`), and read-model property names interpreted as paths or colliding after camel-casing. `$eventSourceId` mappings in joins, in `from` blocks keyed by an event property, into a target not exactly matching the identifier's type, or into an enumerated concept also fail `STAGE-ESM-017`. Do not claim a shape is portable without an engine-backed fix and a passing differential check.

The Cratis renderer renders contract and production tags in that order, and rejects duplicate tags, because Chronicle de-duplicates them, and guarded productions (`STAGE-ESM-006`). Portable single-text-property and unique event-occurrence constraints render as named Chronicle constraints, including multiple event targets, releases, casing, and messages. A constrained command input receives a not-null validator; optional, composite, and nontext property keys and commands producing multiple target or release events in one batch fail admission (`STAGE-ESM-014`): Chronicle #4122 indexes null incorrectly, its string hashing cannot preserve every typed/composite value, and its batch claims do not follow the reference's intra-command release/replacement behavior. Specifications with givens that violate a constraint fail with `STAGE-ESM-011`; accepted and rejected generated specs seed relevant givens through the event log and assert committed events separately from failed append attempts. Chronicle #4123 means index-update failures after commit currently cannot provide an atomicity guarantee. Portable policy expressions on commands and queries render as registered Arc authorization policies, including role-only and claim-only expressions. Arc's default authenticated boundary denies guest callers. For each protected operation, Stage opts its generated policy into anonymous evaluation only when the effective authorization can be definitely true for a guest with no roles or claims. This includes `not role "Banned"` and `not authenticated`; the policy still evaluates the request and denies an unknown claim target. A statically nontext comparison remains unknown even for a guest, and cannot by itself justify anonymous evaluation under `not`. Stage refuses unauthenticated caller fixtures that carry roles or claims (`STAGE-ESM-011`), because Arc supplies an empty guest principal. Claim types compare case-insensitively, while claim values compare ordinally with the target's text; any matching claim suffices. Text and Uuid targets render, including Guid-backed concepts. A Uuid target uses canonical lowercase, hyphenated `D` text, so uppercase, braced, and unhyphenated claim values deny even when they identify the same Guid. Missing targets deny; an empty text target matches an empty claim. A policy can exclude callers with `not` (ESM v7, Screenplay 4.81). It is evaluated with Screenplay's three-valued logic in both the generated policy and Stage's in-memory evaluator: a claim comparison whose target is missing, null, or not text is *unknown*, `not` keeps it unknown, `and`/`or` decide on a definite operand in either order, and a policy whose result is unknown denies. So `not claim "owner" matches owner` denies when `owner` is omitted, rather than allowing it. A policy that does not use `not` retains its two-valued operation expression. Each protected operation renders in its own `{Domain}/GeneratedPolicies/StagePolicy_*.cs` file, in `{Root}.{Domain}.GeneratedPolicies`; its registered Arc policy name includes the normalized domain and matches the operation's authorization attribute. An empty domain preserves the root placement and policy name. Selection-independent `GeneratedPolicies/Policies.cs` holds the registration registry and value helpers, including the three-valued helpers. A number or Boolean target is never a claim value: a positive comparison with it denies, and under `not` it is unknown, so the policy still denies. A Date or DateTime target matches a claim only by Screenplay's canonical text (`yyyy-MM-dd`, or the round-trip DateTime format with `Z` for UTC), written with the invariant culture; any other spelling never matches. Screenplay `csharp`/`file` policy attachments enter ESM v3 as opaque requirements. A whole opaque policy referenced by a rendered command or query renders as its verified body, admitted through the same closed-compilation `pure` gate as reducer transitions (envelope or compile failures `STAGE-ESM-019`, a missing or foreign `PolicyContext` descriptor `STAGE-ESM-021`, symbols outside the allowlist `STAGE-ESM-022`). Each opaque use site's body is compiled in `{Domain}/GeneratedPolicies/PolicyBodies_*.cs` against its own `{Domain}/TypedContexts/TypedContext_*.cs` wrapper with `Subject`, `Identity` and `Occurred`. The legacy `GeneratedPolicies/PolicyBodies.cs` and `TypedContexts/PolicyContext.cs` paths hold selection-independent shared declarations. `Occurred` is Arc's `AuthorizationPolicyContext.ReceivedAt`, the time the command or query was received; Stage never substitutes the current time. `Identity` is mapped from Arc's claims principal: `IsAuthenticated`, `Roles` (the identity's role claims, compared ordinally like `require role`), `Claims`, and `HasRole`, `HasClaim`, `ClaimValue`, `ClaimValues` (claim names ignore case). As in Screenplay, roles and claims stay separate: a claim of the identity's role claim type appears only in `Roles` and `HasRole`, never in `Claims` or the claim lookups. `Subject` is the canonical text of the command identifier or query key (text, Uuid, Date or DateTime), or empty when Screenplay marks it unavailable. A body that reads `context.Tenant`, the dynamic `context.Artifact` (or `ArtifactAs`), or `context.Identity.Id`, `Name` or `UserName`, which have no defined mapping from the principal, fails `STAGE-ESM-015`, as does a policy that nests an opaque predicate inside a condition (Screenplay's model already refuses one under `not`). Opaque and portable policies compose in authored order with C# short-circuiting, matching the reference evaluator's ordered gates. If a reached opaque term has no receipt time or an unresolvable subject, the whole authorization denies; the term is never treated as false, so a later `or` cannot rescue it. An opaque policy never opts an operation into guest evaluation; only a portable alternative can. A specification of an operation whose authorization depends on an opaque policy fails `STAGE-ESM-011`, because the reference evaluator returns Unsupported for it. For keyed queries, `PolicyContext.Subject` is already defined by Screenplay as the single query key; Stage's portable `claim … matches subject` uses that same keyed argument. Portable caller fixtures and `then denied` run against the generated policy and check its registration; claims using the role claim URI fail admission (`STAGE-ESM-011` for fixtures, `STAGE-ESM-015` for policies) because Arc merges them with roles while Screenplay keeps them distinct. Without these admission checks, the application could append events the reference evaluator rejects, tag them differently, or let unauthorized callers through.

The generated policy `Identity` is a read-only authorization view, not a replacement for `Cratis.Screenplay.Contexts.Identity`. It is sealed, omits `Id`, `Name`, `UserName` and `NotSet`, and exposes `Roles`, `Claims` and `ClaimValues` as immutable arrays rather than `IEnumerable` sequences. The role and claim helper comparisons match Screenplay's identity. Bodies may read the supplied values but cannot name, construct, copy with `with`, or default Stage's policy runtime types; admission refuses those operations with `STAGE-ESM-015`, including target-typed `new` and default literals.

Rendered role-only policies read roles through each identity's `RoleClaimType`. A token carrying roles under another claim type, such as JWT `roles`, is denied unless you map that claim type to the identity's role claim type.

ESM v3 (`3.0` language and semantic versions) adds implementation attachments. `SemanticModelLoader.LoadFromPathAsync` loads referenced files from the model root using Screenplay's `AttachmentFiles.Load`; it carries inline and file bodies by requirement id in `LoadedSemanticModel.ImplementationContents` and compiler metadata in `ImplementationRequirements`. A pure `ArtifactRenderRequest` can carry both via its additive init properties, or callers can use the additive `CratisRendering.Plan(model, executionPlan, scope, options, requirements, contents)` overload. The planner verifies the whole supplied envelope, including requirements outside the selected render scope: each requirement must have a resolved body matching its SHA-256 `ContentHash`, and orphan bodies fail with `STAGE-ESM-020`. It also rejects missing requirements for model-referenced code validations, rule predicates, reducer transitions and opaque policies. Requests without attachments carry no bodies; the old four-argument overload cannot render body-bearing constructs. The planner cannot establish whether an internally consistent envelope came from the same compilation as the model. Pass `LoadedSemanticModel.AttachmentDiagnostics` to the seven-argument overload (or set `ArtifactRenderRequest.AttachmentDiagnostics`) to include PLAY043x file-refusal reasons in `STAGE-ESM-020`. For a single `.play` input, the attachment root is its entire parent directory; use a dedicated folder to narrow the files eligible for loading. Do not supply file paths as content identities or reuse bodies from a previous compilation.

Stage still rejects command code validations, named rule predicates, and concept code validations (`STAGE-ESM-005`): Arc does not supply the received-at `RuleContext.Occurred` to a generated validator today, and Stage does not yet enforce the required `pure` capability. Arc's scoped validator service provider permits tenant and caller collaborators as in Stage's command handler; using the validation clock in place of received-at would change what code can observe. Reducer transitions with verified C# bodies and wrapper-ready descriptors render as Chronicle `IReducerFor<T>` methods. The body uses generated PascalCase C# members (`context.Event.Amount`, not authored lowercase names). A member-casing compile error names the generated member and maps back to the inline `.play` line/column or attachment file line/column; Stage never rewrites the body. The body returns nullable state (`null` deletes), and must satisfy Stage's closed-compilation `pure` symbol allowlist; forbidden symbols report `STAGE-ESM-022`. Event source identity supplies the key: a reducer requires a matching keyed lookup query and a required Guid/Text identifier (or identifier concept). Each public generated `On` compares every non-null result's identifier, including both concept `TypedValue` and `Value`, with the original event-source string using ordinal equality and Chronicle's canonical Guid conversion; a mismatch throws and discards the batch. A noncanonical UUID source string is not silently normalized. Null still deletes. This guards the emitted write, not just a later query lookup (which cannot detect a record stored under another source). `Occurred` and checked `SequenceNumber` come directly from Chronicle's event context. The wrapper permits `context.Tenant` reads and translates Chronicle's `Default` namespace to the portable zero-GUID `TenantId.Default`, and Chronicle's `NotSet` sentinel to `TenantId.NotSet`. Named namespaces retain their exact spelling and casing. The command runtime and generated command handlers honor Arc's `IsDefault`: both named `Default` and unresolved `NotSet` map to the portable zero GUID. A request without a tenant header is the ordinary single-tenant case and addresses Chronicle's default namespace, so its command and reducer see the same portable tenant. Chronicle's explicit `NotSet` namespace remains the empty portable sentinel. Named tenants still use the shared translation implementation. An empty named namespace or a namespace parsing as the zero GUID collides with a portable sentinel and throws before executing a tenant-reading body; it is never collapsed into the default tenant. Reducers that do not read `Tenant` skip namespace translation. Bodies can compare `context.Tenant` with `TenantId.Default` and `TenantId.NotSet`. Scoped renders emit only wrappers consumed by selected reducers; other descriptors remain validate-only. Reducer read-model specs replay only matching events from the expected key; specs depending on `Occurred`, `SequenceNumber` or `Tenant` and action/query expectations against reducer-built models fail `STAGE-ESM-011`. A query-only specification of one complete seeded read model can still be admitted: Chronicle's `ReadModelScenario` seeds that lookup directly, rather than replaying the reducer. ESM v3 cannot express absence in `then readmodel`, so generated specs assert only representable presence and values; the Stage-side `ReducerDeletion` target conformance test asserts delete-by-null against Chronicle's `ReadModelScenario`. A Screenplay absence expectation is needed before this can be a generated specification. Unsupported shapes and body compile failures still fail closed with `STAGE-ESM-019`; a missing, foreign or non-ready descriptor gives `STAGE-ESM-021`. The `pure` gate admits **finite evaluation assuming sufficient resources and deterministic results across machines on the same .NET runtime major version**, rather than arbitrary C#. Exceptions are deterministic outcomes that discard the Chronicle batch; there is no bounded-time, bounded-memory, or recovery-from-resource-exhaustion promise. The audited subset consists of fixed-width integer and decimal arithmetic (checked or unchecked), bool, char, strings, literals, locals, null-coalescing/conditional access, constant patterns, generated record constructors/`with`/property reads and generated-type tests; `if`/`else`, constant `switch`, `return` and finite `foreach`; and throws constructed only as `InvalidOperationException(string)` or `ArgumentException(string)`. The audited Math signatures are `Abs(int/decimal)`, `Min`/`Max`/`Clamp`/`Sign(decimal)`, and `Round(decimal, MidpointRounding.ToEven)`, `Truncate`/`Floor`/`Ceiling(decimal)`; other overloads are refused. Text admits string/char-only concatenation (including `+=`), `Length`, indexing, `==`, `!=`, `Substring(int, int)`, `IsNullOrEmpty`, and `Equals`, `StartsWith`, `EndsWith`, `Contains`, `IndexOf` with the *constant field* `StringComparison.Ordinal`; Unicode-dependent casing and whitespace classification (including `OrdinalIgnoreCase`, `Trim`, and `IsNullOrWhiteSpace`) are excluded; only decimal `ToString(CultureInfo.InvariantCulture)` is admitted for numeric formatting. The audited time signatures are `DateTimeOffset.AddTicks(long)`, `DateTimeOffset + TimeSpan`, `DateTimeOffset - DateTimeOffset`, `DateTimeOffset >= DateTimeOffset`, and direct `DateTimeOffset.Year`/`Month`/`Day`/`Hour`/`Minute`/`Second`/`Ticks`; `TimeSpan.Add(TimeSpan)`, `TimeSpan == TimeSpan`, and `TimeSpan.Ticks`. Other time operations (including `UtcDateTime`, `LocalDateTime`, `Offset` and double-taking additions) are refused.

Reducer-visible collection inputs are rendered as `ImmutableArray<T>` rather than interfaces that could reveal mutable lists or arrays; nested composites containing collection interfaces are refused until their whole closure can be snapshotted. Local finite arrays and immutable arrays can be read or enumerated, but not mutated. The exact fixture-covered signatures include `Select`, `Where`, predicate `Count`/`Any`, `All`, decimal-only sequential `Sum` (verified against .NET 10 source), `First`/`FirstOrDefault`, `Last`/`LastOrDefault`, `Take`, `Skip`, `Concat`, `ToImmutableArray`, `ToArray`, and seeded `Aggregate`; integer `Sum` is refused because vector width changes overflow outcomes (use seeded `Aggregate` with explicit unchecked integer addition instead). The roster is an exact BCL-member-signature allowlist: adding an audited overload requires a fixture. LINQ calls cannot consume a source produced by another LINQ call, even through a local; parameterless `Any` and `Count` are refused. This deliberately avoids selector elision differences from `IsSizeOptimized` and other `AppContext` switches. Lambda bodies must be a single pure expression over their own parameters and the typed context: no assignments/increments, captured locals or non-context parameters (including enumerables), nested LINQ, divide/modulo, checked operations, potentially overflowing decimal arithmetic or narrowing decimal conversions, array indexing, throws or other non-ordinal invocations. Enumerable locals are single-assignment. Hash collections, sorting, `Distinct`, `GroupBy`, `OrderBy`, floating and native-sized numbers, `BigInteger`, `DateTime`, parsing, ambient culture/time/randomness, implicit user conversions, arbitrary casts, `typeof`, `nameof`, `sizeof`, non-generated `default`, local functions, unbounded loops, `goto`, `try`, `using`, and `lock` are refused with `STAGE-ESM-022`, naming the construct or symbol. UUID identity properties can be read from generated event records, but `Guid.Empty`, parsing, constructors and other Guid members are refused. The gate is not an OS sandbox. It rejects any physical CR in a body before reducer analysis or emitted-artifact hashing because `PlannedArtifact.CreateText` normalizes CR to LF even inside verbatim strings; the attachment envelope hashes the original candidate **before** reducer analysis and CR refusal. Accepted body bytes therefore equal emitted body bytes.

Production admission uses the rendered reducer's containing class name, namespace, transition member name and usings plus SDK implicit BCL globals and synthetic generated declarations against the exact `Microsoft.NETCore.App.Ref` **10.0.12** pack for `net10.0`; a missing pack fails closed, with no new Release dependencies. Generated types and namespace segments that could rebind unqualified type references in a reducer body are refused, as are generated declarations shadowing reducer runtime types. Synthetic concepts deliberately omit implicit conversions: any mismatch refuses more, never less. Render profiles are pinned per Stage version, so the Debug CI parity compilation with the pinned real Cratis references compares **full bound member identities** (owner, kind, name, generic arity, parameter and result types, including implicit `foreach` GetEnumerator/MoveNext/Current) of every audited fixture against the production compilation for the same rendered file shape. Every admitted BCL member signature must appear in a parity fixture; new members without fixtures are refused. Chronicle batches events per key, threads null state into the next event (`IsFirst` becomes true), and persists only final batch state: a delete followed by recreation is not an intermediate materialization. A thrown transition discards the entire batch. A resumed batch straddling the watermark can be folded twice, so non-idempotent pure bodies can double-count; this is a Chronicle issue, not a Stage retry policy. Chronicle's exact runtime read-model-type check is satisfied by the generated `T?` return type; statement bodies cannot declare a subtype. Whole opaque policies are admitted through the same gate against their own namespace and signature (see above). A plan that rejects a body emits no artifacts. A v3 model with only an unreferenced opaque policy and no reducers renders the same selected slice bytes as v2. The in-memory runtime and specification executor leave a reached opaque policy unsupported (Authorization capability), preserving left-to-right short-circuit outcomes rather than guessing allow or deny. Unknown version pairs still fail `STAGE-ESM-016`.

Screenplay 4.41 adds a *sidecar*, not new ESM bytes. `LoadedSemanticModel.TypedContextDescriptors` carries descriptors alongside requirements and bodies; use the eight-argument `CratisRendering.Plan(model, executionPlan, scope, options, requirements, contents, attachmentDiagnostics, descriptors)` overload, or set `ArtifactRenderRequest.TypedContextDescriptors` and `TypedContextContractRevision`. Always pass the three collections from **one** compilation. Stage checks its supported contract revision 1, requirement identity/role/context version, wrapper readiness, and the model revision; a mismatch or unknown token/type/source kind fails closed with `STAGE-ESM-021`. Version admission runs first: an unsupported version pair receives `STAGE-ESM-016`, and a v5–v7 construct Stage does not render (below) refuses the plan, even with a bad descriptor; otherwise envelope diagnostics precede rendering validation. The Host and CLI do not forward descriptors yet; only direct API callers can supply them. Wrapper rendering validates fail-closed and emits `{Domain}/TypedContexts/` artifacts in `{Root}.{Domain}.TypedContexts` only for reducer bodies consumed by selected slices. Stage emits its own parity-pinned `TenantId` runtime token for reducers under `{Root}.TypedContexts`, without adding the Screenplay compiler to the generated app. `Property` remains a constructor argument even when its source carries `ConstantValue`: without a consumer, Stage has no admitted runtime binding for that argument; #119 must decide whether to bake it into the generated wrapper or supply it when constructing one. Do not reconstruct a descriptor from an ESM-only export or reuse one across compilations. The C# generator maps `Text` to `string`, `WholeNumber` to `long`, `Boolean` to `bool`, `DateTime` to `DateTimeOffset`, and `TenantId`, `Identity`, `CausedBy`, `Causation` to their `Cratis.Screenplay.Contexts` types. Shaped members name generated command, read-model or current-event types (query arguments get a local shape); model types name generated concepts/composites, including collections and optionality. Nullable state and optional rule values stay nullable, and `IsFirst` / `IsWholeArtifact` are computed rather than constructor parameters. Never substitute `dynamic`. ESM v4 descriptors may bind only the current event shape; historical event sources and properties fail with `STAGE-ESM-025`. Stage has no virtual C# editor-document surface today: it cannot yet hand these wrappers to an editor or source-map embedded code positions to one.

ESM v4 (`4.0` language and semantic versions) is admitted by the version gates, but evolved event contracts are not renderable yet. `SemanticEventContract.Revision` is carried for admission rather than emitted as a Chronicle generation. An initial-revision event renders exactly as before, with the bare `EventTypeAttribute` and the unchanged type-id mapping. Any selected event or event dependency whose revision exceeds the initial one fails with `STAGE-ESM-026`, naming the event and revision: "Event 'ProjectRegistered' has evolved to revision 2; Stage does not render event migrations yet." No artifacts are emitted. This includes RegisterProject/v2, commands producing evolved events, and projections or reducers observing an evolved event declared outside the selected slice. A scoped initial-event slice in a v4 model can render when it does not depend on an evolved event.

`Predecessor` and `PriorRevisions` remain carried lineage metadata. Historical `Revision`, `Predecessor`, `Properties`, and `Tags` never render as the current shape. Historical typed-context revision/property references still fail with `STAGE-ESM-025` before event-migration admission; ordinary historical consumption remains refused by Screenplay's binder. ESM v1–v3 artifact bytes are unchanged.

Evolution waits for migration modeling and rendering (Screenplay #71 and a Stage migration-rendering follow-up). In both Chronicle 19.8.1 and 19.32.0, registration validation requires migrators for every generation above 1 even on an empty store. Production kernels always validate; the development image can skip validation, permitting unsafe historical replay. Neither path is an admitted realization without migrations, so Stage refuses before emission rather than relying on the kernel configuration. Stage's Host also retains its `EventContract` refusal, "Only the initial event revision can be registered in Chronicle." Its registrar and fact appender remain generation-1-only; an evolved model is not registered or appended.

Stage builds against Screenplay 4.122.0 and admits ESM v1 through v9 by version. Unaudited language/semantic version pairs fail with `STAGE-ESM-016` in the renderer, Host semantic runtime and specification executor. Admitting a version does not admit its constructs: each construct Stage does not render refuses the plan with its own diagnostic before any artifact is planned, so nothing is dropped from the generated application. A model that uses none of them renders exactly as it would at an earlier version.

| Construct | Version | Diagnostic |
|---|---|---|
| A specification asserting a read model is absent (`thenAbsentReadModels`) | v5 | `STAGE-ESM-027` |
| Application triggers, reactions, captures, Automation and Translate slices, and specifications with a clock, trigger or capture | v6 | `STAGE-ESM-024` |
| Generated command properties and specification generation fixtures | v7 | `STAGE-ESM-028` |
| Command responses and `then returns` expectations | v7 | `STAGE-ESM-029` |
| Policy negation (`not`) | v7 | Rendered |
| Named event sources, streams and command routes | v8 | Rendered; reserved stored names fail `STAGE-ESM-030` |
| Routed command specifications | v8 | Rendered: route-aware givens and assertions, assignment matching for any-order expectations; unformattable fixtures or routed read-model/query replay fail `STAGE-ESM-030` |

| Local public events declared or referenced by the selected slice, and event-target projections or reducers | v9 | `STAGE-ESM-031` |
| Events with an origin store and `source events` captures | v9 | `STAGE-ESM-032` |
| Directed Translate slices (inbound or outbound) | v9 | `STAGE-ESM-024`, naming the direction |

Diagnostics apply to the selected scope: a slice-scoped render of a slice that uses none of these constructs can still render, and application triggers refuse only an application render. The in-memory specification executor reports the same constructs as unsupported rather than skipping them. Keep these constructs in the model and use a renderer that explicitly supports them rather than deleting them to pass admission. A specification that supplies a generated property as input never compiles (`PLAY0485`), so Stage plans nothing for it.

Build target-local indexes keyed by `SemanticId`. Use `ExecutableSemanticModel` and `SemanticExecutionPlan` mappings for command production, destinations, properties, projection transitions, affected-instance keys, queries, and typed specification values. Never join artifacts by a short or display name.

## Apply scope semantics consistently

`ArtifactRenderScopeKind` has four admitted values:

- `Application` selects the entire application;
- `Module` selects one module and everything below it;
- `Feature` selects one feature and everything below it, including nested features;
- `Slice` selects one slice.

The accompanying `ArtifactRenderScope.Artifact` must be the matching semantic identity in `request.Model`. `ArtifactRenderPlan.Create(...)` rejects unknown kinds, unset identities, an application scope with the wrong application identity, and module, feature, or slice identities outside the model.

Selection and dependency closure remain target responsibilities. Define whether a narrow scope emits:

- a compilable closure containing every referenced common and target artifact; or
- an intentionally incomplete review fragment.

Do not call a fragment compilable. Admission must include every semantic dependency required by the documented policy. Application scope normally includes application-wide configuration, scaffolding, and common types; narrower scopes should not silently acquire destination-dependent files.

The Cratis target includes the concepts, composite types and authorization policies referenced by selected slices, including transitive composite-type dependencies. Unreferenced shared declarations are excluded from module, feature and slice plans; application plans still include all concepts and composite types. Shared artifacts keep the same paths and bytes across scopes.

Cratis scoped plans target an already-scaffolded repository. They do not include the application scaffold or other slices' event and read-model declarations. Those declarations must already exist when a selected slice references them. Each protected operation has its own `GeneratedPolicies/StagePolicy_*.cs` file; after the policy-layout migration below, the shared policy runtime does not depend on which operations are selected, so regenerating one slice preserves other operations' policies.

**Before your first scoped render after upgrading from the aggregate policy layout, render the entire application.** The old `GeneratedPolicies/Policies.cs` contains every generated policy class and registration. A scoped render replaces that file but creates new policy files only for its selected operations, silently removing unselected operations' policy classes and registrations. An application-scope render migrates every operation and opaque use site to the split layout before subsequent scoped renders. The pure planner does not inspect destination files. Before writing a scoped Cratis plan, publishers must call `CratisRendering.CheckPublication(plan, readExistingFile)`. The reader receives an application-root-relative path and returns its text, or null only when the file is confirmed absent; a read failure must throw so the check fails closed. The typed `CratisPublicationCheck.Compatible` result permits this policy-layout replacement; `CratisPublicationCheck.RequiresApplicationScope` carries the offending `Paths` and a `Reason`. On the latter, refuse publication or render the entire application first.

The check reads only `GeneratedPolicies/Policies.cs`, `GeneratedPolicies/PolicyBodies.cs` and `TypedContexts/PolicyContext.cs` paths that the plan itself would overwrite, using their planned placement. It checks content, not mere file presence: the split layout keeps these legacy paths. Identical planned text (after newline normalization) is compatible; otherwise C# declarations must identify the selection-independent registry and shared declarations, without aggregate `StagePolicy_*` classes, per-site bodies or `TypedContext_*` wrappers. Unrecognized content requires application scope. Application-scope plans are always compatible. A scaffold-only plan never contains policy files, so there is nothing to check.

This check does not replace the publisher's destination ownership, concurrency, recovery or write-failure checks. See [publication checks](../reference/plan-from-sources.md#publication-check) for the signature and caller flow.

Any artifact present in two scopes must have the same normalized path, kind, and exact bytes. Scope changes selection only; they must not change how the same artifact is rendered.

## Produce deterministic artifacts

Create every output in memory with the public factories:

```csharp
var source =
    "export interface RegisterProject {\n" +
    "    name: string;\n" +
    "}\n";

var artifact = PlannedArtifact.CreateText(
    "src/register-project.ts",
    source);
```

`PlannedArtifact.CreateText(...)` normalizes CRLF and CR to LF, encodes UTF-8 without a byte-order mark, and computes a lowercase SHA-256 hash. `PlannedArtifact.CreateBinary(...)` preserves exact bytes and hashes them.

`ArtifactRenderPlan.Create(...)` then:

- verifies the model/execution-plan revision and scope;
- revalidates and normalizes artifact paths;
- orders artifacts by ordinal relative path;
- rejects duplicate paths and case-insensitive collisions;
- deterministically orders diagnostics by severity, code, semantic identity, and message;
- copies target, renderer, application, and semantic revision metadata into the plan.

Artifact paths must be slash-separated and relative. Rooted paths, drive-qualified paths, empty segments, `.`, and `..` are invalid.

The target must also make its own emission deterministic:

- order declarations by stable semantic identity or another documented stable key;
- sort every dictionary, set, and filesystem-derived input before it reaches the request;
- never let enumeration order choose semantics or resolve a conflict;
- keep physical workspace roots and publication destinations out of content;
- resolve all templates and package versions into the profile before planning;
- avoid timestamps, generated random identifiers, machine-specific headers, and formatter-version drift.

The Cratis renderer orders read-model parameter declarations, generated constructor arguments, and assertions by ordinal semantic ID so serializing and deserializing the model does not change artifact bytes. This can change positional record parameter order in directly source-generated output from source declaration order; the captured route already uses canonical order.

A plan describes destination-independent bytes. Publication, staging, stale-file removal, recovery, and overwrite policy belong after successful planning.

## Verify the generated code

Deterministic bytes do not prove that a target toolchain accepts the generated application. Add target-owned verification that:

1. creates a request from trusted Screenplay fixtures and an exact profile;
2. requires successful compilation, execution-plan admission, and target planning;
3. materializes the plan's exact bytes into a fresh temporary directory without rewriting or formatting them;
4. verifies each materialized file against the planned SHA-256;
5. restores only exact pinned target dependencies from trusted sources;
6. runs the target compiler or build with warnings treated as errors;
7. runs focused target-framework tests where compilation cannot prove behavior;
8. repeats in a clean environment to expose ambient dependency assumptions.

Keep formatting and code generation inside the pure planner. A verifier that reformats or patches generated files is testing different artifacts from those in the plan.

Treat generated build definitions as code execution. Verify trusted fixtures in an isolated environment, and do not execute package scripts or build targets from untrusted profile inputs.

## Specify the renderer

At minimum, add specifications for:

1. exact target, renderer, version, and profile-input admission;
2. missing, extra, malformed, or wrong-version profile inputs failing closed;
3. every supported semantic form producing expected paths, kinds, bytes, and hashes;
4. unsupported reachable semantics producing target errors and no dependent artifacts;
5. compiler diagnostics and execution issues remaining owned by their upstream phases;
6. reversed semantic and profile-input enumeration producing byte-identical plans;
7. repeated planning producing byte-identical plans without filesystem, process, network, clock, or environment effects;
8. application, module, feature, and slice scope selection and dependency policy;
9. shared artifacts having identical paths and bytes across scopes;
10. rooted, drive-qualified, empty-segment, and traversing paths being rejected;
11. duplicate and case-insensitive-colliding paths being rejected;
12. changed accepted input bytes deterministically changing every affected artifact;
13. specifications rendering from typed semantic values without string guessing;
14. the exact generated package closure compiling against pinned versions.

Golden files are useful for review, but assert the plan metadata and diagnostics as well as source text. Add invariance tests that perturb input order; one successful snapshot does not prove determinism.

## Integrate through the CLI's static roster

The Cratis CLI uses a static, reviewed roster of bundled targets. It does not discover arbitrary renderer packages from the analyzed workspace. The current CLI wrapper contract and roster are internal CLI implementation seams, not a public plugin API.

To bundle a target, coordinate a CLI change that:

1. adds a target package reference and an internal target wrapper under `cli/Source/Cli/Commands/Render`;
2. gives the wrapper a stable command-line target name;
3. constructs the exact immutable profile and resolved inputs;
4. calls the target planner with the admitted model and execution plan;
5. explicitly adds the wrapper to `RenderTargetRoster`;
6. adds target selection, profile, package-closure, plan, and publication specifications;
7. passes successful plans to the existing managed artifact publisher.

The current CLI command requests application scope. Module, feature, and slice scopes are public planner-contract capabilities, but users cannot select them until the CLI adds explicit command support.

Preserve the static roster. Workspace plugin loading would execute code across a larger trust boundary and allow unreviewed dependency and target-version conflicts.

## Document the support contract

Publish a target support matrix that distinguishes:

- supported Screenplay semantic forms;
- blocked forms and their target diagnostic codes;
- exact target, renderer, compiler, runtime, and package versions;
- exact profile inputs and defaults;
- scope and dependency-closure behavior;
- generated artifact layout and text conventions;
- generated-code verification fixtures;
- known non-bijective source-recovery concerns.

A source adapter recovering generated code can be an additional regression oracle, but it cannot prove behavioral equivalence. Target realization choices that Screenplay does not express will not round-trip.

## Implementation checklist

Before declaring a target ready:

- [ ] Compile the complete logical application with stable document keys.
- [ ] Stop on compiler diagnostics or execution-plan issues before target planning.
- [ ] Define stable target and renderer identities with exact versions.
- [ ] Resolve and hash every external input before creating the profile.
- [ ] Admit the exact profile roster and fail closed on every mismatch.
- [ ] Implement scope selection and document narrow-scope dependency behavior.
- [ ] Index and join semantics by `SemanticId`, never display names.
- [ ] Admit all selected and reachable semantics before dependent emission.
- [ ] Keep `Plan(...)` free of filesystem, process, network, clock, random, and ambient-state access.
- [ ] Emit all artifacts in memory through `PlannedArtifact` factories.
- [ ] Make ordering, names, paths, text encoding, line endings, and hashes deterministic.
- [ ] Preserve diagnostic ownership and use stable target-owned codes.
- [ ] Verify profile, admission, scope, collision, and determinism behavior with specifications.
- [ ] Materialize and compile the exact generated bytes against pinned target dependencies.
- [ ] Document the support matrix, profile, artifact layout, diagnostics, and verification environment.
- [ ] Add the target to the CLI only through its reviewed static roster.

## Public API reference

The renderer-facing public contracts are defined in:

- `Source/Contracts/Rendering/IArtifactRenderPlanner.cs`
- `Source/Contracts/Rendering/ArtifactRenderProfile.cs`
- `Source/Contracts/Rendering/ArtifactRenderPlan.cs`

Use the Screenplay `SemanticModelCompiler` and `SemanticExecutionPlan` APIs shown above to build the request. Renderer-specific files under `Source/Rendering.Cratis` can illustrate one target's decisions, but they are not a shared helper library or an extension contract for other targets.
