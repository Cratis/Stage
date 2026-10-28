// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>The pinned, reversible prerelease score policy; weights do not determine leaf eligibility.</summary>
public static class ArtifactRenderConstructWeights
{
    /// <summary>The score policy and arithmetic version.</summary>
    public const string Version = "stage-render-completeness-score/v1";

    /// <summary>Gets a semantic leaf's possible weight, counted once regardless of its number of causes.</summary>
    /// <param name="kind">A known leaf category.</param>
    /// <returns>The fixed score-policy weight.</returns>
    public static int For(ArtifactRenderConstructKind kind) => kind switch
    {
        ArtifactRenderConstructKind.Unknown or ArtifactRenderConstructKind.Command or ArtifactRenderConstructKind.Operation or
            ArtifactRenderConstructKind.Guard or ArtifactRenderConstructKind.Validation or ArtifactRenderConstructKind.Policy => 5,
        ArtifactRenderConstructKind.Screen or ArtifactRenderConstructKind.Specification => 1,
        _ when Enum.IsDefined(kind) => 3,
        _ => throw new InvalidArtifactRenderContract("Unknown semantic construct kind.")
    };
}
