// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Scaffolding;

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
    /// Creates the complete immutable target profile, including all exact scaffold bytes and hashes.
    /// </summary>
    /// <param name="applicationName">The semantic application name used for persistent stores.</param>
    /// <param name="options">The explicit project and root namespace choices.</param>
    /// <returns>The exact package-owned Cratis profile.</returns>
    /// <exception cref="InvalidCratisBackendApplicationScaffold">Thrown when the application name or explicit options are invalid.</exception>
    public static ArtifactRenderProfile CreateProfile(string applicationName, CratisRenderingOptions options)
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
        var inputs = new CratisBackendApplicationScaffold().Create(request);

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
