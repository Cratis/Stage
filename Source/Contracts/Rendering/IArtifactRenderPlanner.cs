// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// Defines the semantic scope planned by a render request.
/// </summary>
public enum ArtifactRenderScopeKind
{
    /// <summary>
    /// An unknown scope. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// The entire application.
    /// </summary>
    Application = 0,

    /// <summary>
    /// One module and everything below it.
    /// </summary>
    Module = 1,

    /// <summary>
    /// One feature and everything below it.
    /// </summary>
    Feature = 2,

    /// <summary>
    /// One slice.
    /// </summary>
    Slice = 3
}

/// <summary>
/// Defines a pure system that plans target artifacts without writing them.
/// </summary>
public interface IArtifactRenderPlanner
{
    /// <summary>
    /// Plans all artifacts for one immutable semantic request.
    /// </summary>
    /// <param name="request">The semantic model, execution plan, target profile, and requested scope.</param>
    /// <returns>The complete deterministic artifact plan and typed diagnostics.</returns>
    ArtifactRenderPlan Plan(ArtifactRenderRequest request);
}

/// <summary>
/// Represents one semantic planning scope.
/// </summary>
/// <param name="Kind">The scope kind.</param>
/// <param name="Artifact">The application, module, feature, or slice semantic identity.</param>
public sealed record ArtifactRenderScope(ArtifactRenderScopeKind Kind, SemanticId Artifact);

/// <summary>
/// Represents one fully resolved pure artifact-planning request.
/// </summary>
/// <param name="Model">The immutable executable semantic model.</param>
/// <param name="ExecutionPlan">The capability-admitted execution plan for <paramref name="Model"/>.</param>
/// <param name="Profile">The fully resolved target and renderer profile.</param>
/// <param name="Scope">The semantic scope to render.</param>
/// <remarks>Added attachment collections use the collection's reference equality in record comparisons.</remarks>
public sealed record ArtifactRenderRequest(
    ExecutableSemanticModel Model,
    SemanticExecutionPlan ExecutionPlan,
    ArtifactRenderProfile Profile,
    ArtifactRenderScope Scope)
{
    /// <summary>Compiler requirements for the model; the planner verifies their content revisions before using a body.</summary>
    public ImmutableArray<SemanticImplementationRequirement> ImplementationRequirements { get; init; } = [];

    /// <summary>Resolved implementation bodies keyed by stable requirement identity, not by file path.</summary>
    public ImmutableDictionary<string, string> ImplementationContents { get; init; } = [];

    /// <summary>Attachment-loader diagnostics to include with a missing body's render diagnostic.</summary>
    public ImmutableArray<Diagnostic> AttachmentDiagnostics { get; init; } = [];
}
