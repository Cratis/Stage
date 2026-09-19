// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Scene;

/// <summary>
/// The composed Scene payload carried through the render profile.
/// </summary>
/// <remarks>
/// The payload travels as an ordinary scaffold input, so it is hashed, ordered and admitted exactly like every
/// other resolved input, and an application that declares no screens simply has one fewer input.
/// </remarks>
internal static class SceneCompositionInput
{
    /// <summary>
    /// The planned artifact path of the composed Scene payload.
    /// </summary>
    public const string RelativePath = "scene.json";

    /// <summary>
    /// Determines whether the profile carries a composed Scene.
    /// </summary>
    /// <param name="profile">The render profile.</param>
    /// <returns><c language="csharp">true</c> when a Scene payload is present.</returns>
    public static bool IsCarriedBy(ArtifactRenderProfile profile) =>
        profile.Inputs.Any(_ => CratisArtifactRenderInput.TryCreateArtifact(_, out var artifact) &&
            string.Equals(artifact!.RelativePath, RelativePath, StringComparison.Ordinal));
}
