// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

public static partial class CratisRendering
{
    /// <summary>
    /// Restores the explicitly selected projection convention of a new application into its complete profile.
    /// </summary>
    /// <param name="profile">The application profile, including any Scene and reducer inputs.</param>
    /// <param name="input">The input read by <see cref="ProjectionConventionInput.Read"/>.</param>
    /// <returns>The profile with its persisted projection convention.</returns>
    /// <remarks>
    /// Model-bound projections have different observer identities from classic projections. Select this only for new
    /// applications. Absent configuration remains classic; existing applications are not automatically migrated.
    /// </remarks>
    /// <exception cref="InvalidArtifactRenderContract">The input has a different identity.</exception>
    public static ArtifactRenderProfile WithProjectionConvention(ArtifactRenderProfile profile, ArtifactRenderInput input)
    {
        if (input.Name != ProjectionConventionInput.Name)
        {
            throw new InvalidArtifactRenderContract("Expected a projection convention profile input.");
        }

        return ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, [.. profile.Inputs.Where(existing => existing.Name != ProjectionConventionInput.Name), input]);
    }
}
