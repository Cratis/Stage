// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Scene.Engine.Packages;
using Cratis.Scene.Model.Packages;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Cratis.Stage.Rendering.Cratis.Scene;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Shipped;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Represents the explicit destination-independent names selected for a Cratis artifact plan.
/// </summary>
/// <param name="ProjectName">The generated project and solution file name.</param>
/// <param name="RootNamespace">The generated C# root namespace.</param>
public sealed record CratisRenderingOptions(string ProjectName, string RootNamespace)
{
    /// <summary>
    /// Gets the requested gap policy. Refuse is the default. This option has no effect until the target supports gap rendering.
    /// </summary>
    public ImplementationGapMode ImplementationGaps { get; init; } = ImplementationGapMode.Refuse;
}

/// <summary>
/// Provides the complete package-owned Cratis v1 target policy used by every planning caller.
/// </summary>
public static partial class CratisRendering
{
    /// <summary>
    /// The only Stage v1 forward-rendering target identity.
    /// </summary>
    public const string TargetId = "cratis";

    /// <summary>
    /// The stable renderer identity admitted by the planner.
    /// </summary>
    public const string RendererId = "Cratis.Stage.Rendering.Cratis";

    /// <summary>
    /// The exact renderer implementation version.
    /// </summary>
    public const string RendererVersion = "1";

    /// <summary>
    /// Gets the exact Cratis integration target version.
    /// </summary>
    public static string TargetVersion => Dependencies.CratisPackageVersion;

    /// <summary>
    /// Gets the packages this target renders without any approval: the Scene core vocabulary, the Scene web
    /// components, the Cratis component library, the PrimeReact widgets and the default blueprint the Stage frontend ships.
    /// </summary>
    /// <remarks>
    /// This is the catalog a <c language="csharp">ui profile</c> is resolved against. A package outside it is only
    /// renderable when the application approves it with an <see cref="ApprovedPackageSet"/>; use
    /// <see cref="CatalogWith"/> to resolve against both.
    /// </remarks>
    public static IReadOnlyList<ScenePackage> ShippedPackages { get; } =
    [
        .. ShippedPackageManifests.All.Where(package => package.Name == StageShippedPackages.Core),
        new(StageShippedPackages.SceneWeb, "1.0.0", PackageKind.ComponentLibrary, [], [], [], [], [], []),
        new(StageShippedPackages.CratisComponents, Dependencies.FrontendPackageSet.ComponentsPackageVersion, PackageKind.ComponentLibrary, [], [], [], [], [], []),
        .. ShippedPackageManifests.All.Where(package => package.Name == StageShippedPackages.PrimeReact || package.Name == StageShippedPackages.DefaultBlueprint)
    ];

    /// <summary>
    /// Gets the names of <see cref="ShippedPackages"/>, which an approved package may not reuse.
    /// </summary>
    public static IReadOnlyList<string> ShippedPackageNames { get; } = [.. ShippedPackages.Select(package => package.Name)];

    /// <summary>
    /// Gets the exact dependency and runtime pins carried by the current profile.
    /// </summary>
    public static CratisBackendApplicationScaffoldProfile Dependencies => CratisBackendApplicationScaffoldProfile.Current;

    /// <summary>
    /// Gets the catalog a <c language="csharp">ui profile</c> resolves against: the shipped packages and the approved ones.
    /// </summary>
    /// <param name="approvedPackages">The application's approved packages, or null for none.</param>
    /// <returns>The shipped packages followed by the approved packages' manifests.</returns>
    public static IReadOnlyList<ScenePackage> CatalogWith(ApprovedPackageSet? approvedPackages) =>
        [.. ShippedPackages, .. approvedPackages?.Catalog ?? []];

    /// <summary>
    /// Creates the complete immutable target profile, including all exact backend and frontend scaffold bytes and hashes.
    /// </summary>
    /// <param name="applicationName">The semantic application name used for persistent stores.</param>
    /// <param name="options">The explicit project and root namespace choices.</param>
    /// <returns>The exact package-owned Cratis profile.</returns>
    /// <exception cref="InvalidCratisBackendApplicationScaffold">Thrown when the application name or explicit options are invalid.</exception>
    public static ArtifactRenderProfile CreateProfile(string applicationName, CratisRenderingOptions options) =>
        CreateProfile(applicationName, options, null);

    /// <summary>
    /// Creates the complete immutable target profile, carrying a composed Scene alongside the scaffold bytes.
    /// </summary>
    /// <param name="applicationName">The semantic application name used for persistent stores.</param>
    /// <param name="options">The explicit project and root namespace choices.</param>
    /// <param name="scene">The composed Scene application, or <c language="csharp">null</c> for an application that declares no screens.</param>
    /// <returns>The exact package-owned Cratis profile.</returns>
    /// <remarks>
    /// Scene is supplied rather than derived. The caller compiles Screenplay once and already holds both views of
    /// that compile - the executable semantic model and the translated Scene - so re-deriving a screen here would
    /// mean a second translation of the same source. The semantic model carries no screen, layout, view or binding
    /// at all, so it could not be the source of one anyway.
    /// <para>
    /// An application that declares no screens plans exactly what it planned before: no Scene payload, no binding
    /// module, and byte-identical scaffold output.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidCratisBackendApplicationScaffold">Thrown when the application name or explicit options are invalid.</exception>
    public static ArtifactRenderProfile CreateProfile(
        string applicationName,
        CratisRenderingOptions options,
        Contracts.Scene.SceneApplication? scene) => CreateProfile(applicationName, options, scene, null, null);

    /// <summary>
    /// Creates the profile with validated locale resources from companion Screenplay strings files.
    /// </summary>
    /// <param name="applicationName">The semantic application name.</param>
    /// <param name="options">The explicit project and namespace choices.</param>
    /// <param name="scene">The optional composed Scene.</param>
    /// <param name="stringsFiles">Relative file names and original contents of companion .strings files, or null for no catalog.</param>
    /// <param name="defaultLocale">The locale required to contain every referenced message key.</param>
    /// <returns>The immutable Cratis render profile.</returns>
    /// <exception cref="InvalidCratisBackendApplicationScaffold">Thrown when the options or strings input is invalid.</exception>
    /// <exception cref="Contracts.Scene.UnsupportedGuardedScreenAction">Thrown when the Scene contains guarded screen actions.</exception>
    /// <exception cref="Contracts.Scene.UnsupportedGuardedInteraction">Thrown when the Scene contains guarded interaction alternatives.</exception>
    public static ArtifactRenderProfile CreateProfile(
        string applicationName,
        CratisRenderingOptions options,
        Contracts.Scene.SceneApplication? scene,
        IReadOnlyDictionary<string, string>? stringsFiles,
        string? defaultLocale) => CreateProfile(applicationName, options, scene, stringsFiles, defaultLocale, null);

    /// <summary>
    /// Creates the profile for an application whose frontend is also composed with custom runtime packages.
    /// </summary>
    /// <param name="applicationName">The semantic application name.</param>
    /// <param name="options">The explicit project and namespace choices.</param>
    /// <param name="scene">The optional composed Scene.</param>
    /// <param name="stringsFiles">Relative file names and original contents of companion .strings files, or null for no catalog.</param>
    /// <param name="defaultLocale">The locale required to contain every referenced message key.</param>
    /// <param name="approvedPackages">The custom runtime packages the application approved, or null for none.</param>
    /// <returns>The immutable Cratis render profile.</returns>
    /// <remarks>
    /// The set travels as the scaffold input <c language="shell">approved-packages.json</c>, and the frontend that installs and loads
    /// those packages is generated from it, so admission can regenerate both from the document and refuse a profile that
    /// differs. An application with no approved packages plans exactly what it planned before.
    /// </remarks>
    /// <exception cref="InvalidCratisBackendApplicationScaffold">Thrown when the options, strings input or approved packages are invalid.</exception>
    /// <exception cref="InvalidArtifactRenderContract">The Scene contains conversion errors.</exception>
    public static ArtifactRenderProfile CreateProfile(
        string applicationName,
        CratisRenderingOptions options,
        Contracts.Scene.SceneApplication? scene,
        IReadOnlyDictionary<string, string>? stringsFiles,
        string? defaultLocale,
        ApprovedPackageSet? approvedPackages)
    {
        if (options is null)
        {
            throw new InvalidCratisBackendApplicationScaffold("Cratis rendering requires explicit project and root namespace options.");
        }

        if (scene is not null)
        {
            var errors = scene.ConversionDiagnostics.Where(diagnostic => diagnostic.Severity == Screenplay.Diagnostics.DiagnosticSeverity.Error).ToArray();
            if (errors.Length > 0)
            {
                throw new InvalidArtifactRenderContract(string.Join(Environment.NewLine, errors.Select(error => $"{error.Code}: {error.Message}")));
            }
            Contracts.Scene.SceneRuntimeAdmission.RequireSupported(scene);
        }

        var request = CratisBackendApplicationScaffoldRequest.Create(
            applicationName,
            options.ProjectName,
            options.RootNamespace,
            Dependencies);
        if (approvedPackages is { IsEmpty: false })
        {
            request = request.WithApprovedPackages(approvedPackages);
        }

        var backend = new CratisBackendApplicationScaffold().Create(request);
        var frontend = new CratisFrontendApplicationScaffold().Create(request);
        var composed = scene is null
            ? []
            : new[]
            {
                CratisArtifactRenderInput.CreateText(
                    SceneCompositionInput.RelativePath,
                    Dependencies.CratisPackageVersion,
                    CanonicalSceneJson.Serialize(scene))
            };
        if ((stringsFiles is null) != (defaultLocale is null))
        {
            throw new InvalidCratisBackendApplicationScaffold("Both strings files and a default locale are required together.");
        }

        var strings = stringsFiles is null ? Enumerable.Empty<ArtifactRenderInput>() :
            [StringsCatalogInput.Create(stringsFiles, defaultLocale!)];
        var inputs = backend
            .Concat(frontend)
            .Concat(composed)
            .Concat(strings)
            .OrderBy(input => input.Name, StringComparer.Ordinal)
            .ToImmutableArray();

        return ArtifactRenderProfile.Create(
            TargetId,
            TargetVersion,
            RendererId,
            RendererVersion,
            inputs);
    }

    /// <summary>
    /// Adds declaring-slice screen partitions from the compilation that owns the profile's translated Scene.
    /// </summary>
    /// <param name="profile">The resolved profile carrying the translated Scene when screens are authored.</param>
    /// <param name="compilation">The compilation whose syntax and slice identities own the screens.</param>
    /// <returns>The profile with versioned, hashed screen partitions.</returns>
    /// <exception cref="InvalidArtifactRenderContract">The Scene and compilation disagree or ownership cannot be resolved uniquely.</exception>
    public static ArtifactRenderProfile WithScreenPartitions(ArtifactRenderProfile profile, SemanticCompilation compilation)
    {
        var input = ScreenPartitionsInput.Create(profile, compilation);

        return ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, [.. profile.Inputs.Where(existing => existing.Name != ScreenPartitionsInput.Name), input]);
    }

    /// <summary>
    /// Adds syntax-only documentation from the same compilation as a versioned, hashed renderer input.
    /// </summary>
    /// <param name="profile">The resolved profile, including any Scene or strings inputs.</param>
    /// <param name="compilation">The compilation whose syntax and semantic identities own the metadata.</param>
    /// <returns>The profile with documentation included, or the original profile when none is authored.</returns>
    /// <exception cref="InvalidArtifactRenderContract">Thrown when the compilation metadata is inconsistent or contains invalid XML characters.</exception>
    public static ArtifactRenderProfile WithAuthoringMetadata(ArtifactRenderProfile profile, SemanticCompilation compilation)
    {
        var input = AuthoringMetadataInput.Create(compilation);
        var inputs = profile.Inputs.Where(existing => existing.Name != AuthoringMetadataInput.Name).ToImmutableArray();
        return input is null && inputs.Length == profile.Inputs.Length ? profile : ArtifactRenderProfile.Create(
            profile.Target,
            profile.TargetVersion,
            profile.Renderer,
            profile.RendererVersion,
            input is null ? inputs : inputs.Add(input));
    }

    /// <summary>
    /// Plans exact Cratis artifacts without file-system, process, network, environment, random, or clock access.
    /// </summary>
    /// <param name="model">The immutable executable semantic model.</param>
    /// <param name="executionPlan">The capability-admitted execution plan for <paramref name="model"/>.</param>
    /// <param name="scope">The explicit application, module, feature, or slice scope.</param>
    /// <param name="options">The explicit project and root namespace choices.</param>
    /// <returns>The complete deterministic artifact plan.</returns>
    /// <remarks>
    /// A caller that authored screens passes its own composition to <see cref="CreateProfile(string, CratisRenderingOptions, Contracts.Scene.SceneApplication?)"/>.
    /// A caller that did not gets a default screen composed from the model itself, so a generated application
    /// is usable without hand-writing one first.
    /// </remarks>
    public static ArtifactRenderPlan Plan(
        ExecutableSemanticModel model,
        SemanticExecutionPlan executionPlan,
        ArtifactRenderScope scope,
        CratisRenderingOptions options)
    {
        var profile = CreateProfile(model.Application.Name, options);
        var request = new ArtifactRenderRequest(model, executionPlan, profile, scope);

        return new CratisArtifactRenderPlanner().Plan(request);
    }

    /// <summary>Plans artifacts with compiler requirements and resolved implementation bodies.</summary>
    /// <param name="model">The executable semantic model.</param>
    /// <param name="executionPlan">The admitted execution plan.</param>
    /// <param name="scope">The requested semantic scope.</param>
    /// <param name="options">The project and namespace options.</param>
    /// <param name="requirements">The requirements emitted for this compilation.</param>
    /// <param name="contents">Resolved bodies keyed by requirement identity.</param>
    /// <returns>The deterministic render plan, or diagnostics if an attachment is stale or missing.</returns>
    public static ArtifactRenderPlan Plan(
        ExecutableSemanticModel model,
        SemanticExecutionPlan executionPlan,
        ArtifactRenderScope scope,
        CratisRenderingOptions options,
        ImmutableArray<SemanticImplementationRequirement> requirements,
        ImmutableDictionary<string, string> contents)
        => Plan(model, executionPlan, scope, options, requirements, contents, []);

    /// <summary>Plans with the loader's attachment diagnostics preserved for refused files.</summary>
    /// <param name="model">The executable semantic model.</param>
    /// <param name="executionPlan">The admitted execution plan.</param>
    /// <param name="scope">The requested semantic scope.</param>
    /// <param name="options">The project and namespace options.</param>
    /// <param name="requirements">The compilation's requirements.</param>
    /// <param name="contents">Resolved bodies keyed by requirement identity.</param>
    /// <param name="attachmentDiagnostics">The attachment loader's diagnostics.</param>
    /// <returns>The render plan or blocking diagnostics.</returns>
    public static ArtifactRenderPlan Plan(
        ExecutableSemanticModel model,
        SemanticExecutionPlan executionPlan,
        ArtifactRenderScope scope,
        CratisRenderingOptions options,
        ImmutableArray<SemanticImplementationRequirement> requirements,
        ImmutableDictionary<string, string> contents,
        ImmutableArray<Diagnostic> attachmentDiagnostics)
        => Plan(model, executionPlan, scope, options, requirements, contents, attachmentDiagnostics, []);

    /// <summary>Plans with attachment diagnostics and typed contexts from one compilation.</summary>
    /// <param name="model">The executable semantic model.</param>
    /// <param name="executionPlan">The admitted execution plan.</param>
    /// <param name="scope">The requested semantic scope.</param>
    /// <param name="options">The project and namespace options.</param>
    /// <param name="requirements">The compilation's requirements.</param>
    /// <param name="contents">Resolved bodies keyed by requirement identity.</param>
    /// <param name="attachmentDiagnostics">The attachment loader's diagnostics.</param>
    /// <param name="descriptors">Typed contexts from the same compilation.</param>
    /// <returns>The render plan or blocking diagnostics.</returns>
    public static ArtifactRenderPlan Plan(
        ExecutableSemanticModel model,
        SemanticExecutionPlan executionPlan,
        ArtifactRenderScope scope,
        CratisRenderingOptions options,
        ImmutableArray<SemanticImplementationRequirement> requirements,
        ImmutableDictionary<string, string> contents,
        ImmutableArray<Diagnostic> attachmentDiagnostics,
        ImmutableArray<SemanticTypedContextDescriptor> descriptors)
    {
        var profile = CreateProfile(model.Application.Name, options);
        var request = new ArtifactRenderRequest(model, executionPlan, profile, scope)
        {
            ImplementationRequirements = requirements,
            TypedContextDescriptors = descriptors,
            ImplementationContents = contents,
            AttachmentDiagnostics = attachmentDiagnostics
        };
        return new CratisArtifactRenderPlanner().Plan(request);
    }
}
