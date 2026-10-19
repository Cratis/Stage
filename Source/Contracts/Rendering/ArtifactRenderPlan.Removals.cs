// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Contracts.Rendering;

public sealed partial class ArtifactRenderPlan
{
    /// <summary>
    /// Gets the destination files a publisher must delete atomically with this plan's writes, in ordinal path order.
    /// Empty preserves the original digest format. A publisher that cannot honor a removal must refuse the plan.
    /// </summary>
    public ImmutableArray<ArtifactRemoval> RequiredRemovals { get; }

    /// <summary>
    /// Creates a plan with explicit omitted-artifact refusals, preserving the original diagnostics.
    /// </summary>
    /// <param name="request">The resolved request.</param>
    /// <param name="artifacts">The rendered artifacts.</param>
    /// <param name="diagnostics">The original diagnostics.</param>
    /// <param name="implementationGaps">The implementation gaps.</param>
    /// <param name="refusals">The omitted artifacts and their refusal codes.</param>
    /// <returns>The deterministic plan.</returns>
    /// <exception cref="InvalidArtifactRenderContract">The metadata is malformed or a rendered source was refused.</exception>
    public static ArtifactRenderPlan Create(
        ArtifactRenderRequest request,
        ImmutableArray<PlannedArtifact> artifacts,
        ImmutableArray<ArtifactRenderDiagnostic> diagnostics,
        ImmutableArray<ArtifactImplementationGap> implementationGaps,
        ImmutableArray<ArtifactRenderRefusal> refusals) => Create(request, artifacts, diagnostics, implementationGaps, refusals, []);

    /// <summary>
    /// Computes the output digest, adding refusals-v1 only when refusals are nonempty.
    /// </summary>
    /// <param name="target">The target identity.</param>
    /// <param name="targetVersion">The target version.</param>
    /// <param name="renderer">The renderer identity.</param>
    /// <param name="rendererVersion">The renderer version.</param>
    /// <param name="applicationName">The application name.</param>
    /// <param name="artifacts">The output artifacts.</param>
    /// <param name="diagnostics">The output diagnostics.</param>
    /// <param name="implementationGaps">The implementation gaps.</param>
    /// <param name="refusals">The omitted-artifact refusals.</param>
    /// <returns>The lowercase SHA-256 digest.</returns>
    /// <exception cref="InvalidArtifactRenderContract">The metadata is malformed.</exception>
    public static string ComputeDigest(
        string target,
        string targetVersion,
        string renderer,
        string rendererVersion,
        string applicationName,
        ImmutableArray<PlannedArtifact> artifacts,
        ImmutableArray<ArtifactRenderDiagnostic> diagnostics,
        ImmutableArray<ArtifactImplementationGap> implementationGaps,
        ImmutableArray<ArtifactRenderRefusal> refusals) =>
        ComputeDigest(target, targetVersion, renderer, rendererVersion, applicationName, artifacts, diagnostics, implementationGaps, refusals, []);
}
