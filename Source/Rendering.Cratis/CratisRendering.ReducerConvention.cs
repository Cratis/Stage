// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

public static partial class CratisRendering
{
    /// <summary>
    /// Creates a profile for a confirmed new application, explicitly selecting self-reducing read models and model-bound projections, including running totals and clears.
    /// </summary>
    /// <param name="applicationName">The semantic application name.</param>
    /// <param name="options">The explicit project and namespace choices.</param>
    /// <returns>The profile whose scaffold persists the self and model-bound conventions.</returns>
    /// <remarks>New applications select version 4 of the configuration; existing version 3 applications keep their projection identities. Existing applications keep fluent projections because a model-bound projection's identity is its read model's full name. This API does not migrate existing reducers.</remarks>
    public static ArtifactRenderProfile CreateNewApplicationProfile(string applicationName, CratisRenderingOptions options) =>
        WithReducerConvention(CreateProfile(applicationName, options), ReducerConventionInput.Create(applicationName, ReducerConvention.Classic with { SelfDefault = true, ModelBoundProjections = true, ModelBoundOperations = true }));

    /// <summary>
    /// Adds an explicitly resolved application reducer convention to a profile, including profiles carrying Scene inputs.
    /// </summary>
    /// <param name="profile">The complete application profile.</param>
    /// <param name="input">The input returned by <see cref="ReducerConventionInput.Read"/>.</param>
    /// <returns>The profile carrying the versioned convention. Planning validates its application identity.</returns>
    /// <exception cref="InvalidArtifactRenderContract">The supplied input has a different identity.</exception>
    public static ArtifactRenderProfile WithReducerConvention(ArtifactRenderProfile profile, ArtifactRenderInput input)
    {
        if (input.Name != ReducerConventionInput.Name)
        {
            throw new InvalidArtifactRenderContract("Expected a reducer convention profile input.");
        }

        return ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, [.. profile.Inputs.Where(existing => existing.Name != ReducerConventionInput.Name), input]);
    }
}
