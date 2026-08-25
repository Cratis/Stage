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
