// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Cratis.Stage.Rendering.Cratis.Scene;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Represents the explicit destination-independent names selected for a Cratis artifact plan.
/// </summary>
/// <param name="ProjectName">The generated project and solution file name.</param>
/// <param name="RootNamespace">The generated C# root namespace.</param>
public sealed record CratisRenderingOptions(string ProjectName, string RootNamespace);

/// <summary>
/// Provides the complete package-owned Cratis v1 target policy used by every planning caller.
/// </summary>
public static class CratisRendering
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
    /// Gets the exact dependency and runtime pins carried by the current profile.
    /// </summary>
    public static CratisBackendApplicationScaffoldProfile Dependencies => CratisBackendApplicationScaffoldProfile.Current;

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
    public static ArtifactRenderProfile CreateProfile(
        string applicationName,
        CratisRenderingOptions options,
        Contracts.Scene.SceneApplication? scene,
        IReadOnlyDictionary<string, string>? stringsFiles,
        string? defaultLocale)
    {
        if (options is null)
        {
            throw new InvalidCratisBackendApplicationScaffold("Cratis rendering requires explicit project and root namespace options.");
        }

        var request = CratisBackendApplicationScaffoldRequest.Create(
            applicationName,
            options.ProjectName,
            options.RootNamespace,
            Dependencies);
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
}
