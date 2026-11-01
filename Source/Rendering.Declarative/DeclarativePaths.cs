// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Kits;

namespace Cratis.Stage.Rendering.Declarative;

/// <summary>
/// Validates rendered output paths before any artifact is created.
/// </summary>
static class DeclarativePaths
{
    /// <summary>
    /// Checks one rendered path.
    /// </summary>
    /// <param name="path">The rendered relative path.</param>
    /// <param name="seen">The paths already rendered, compared ignoring case.</param>
    /// <returns>The refusal, or <see langword="null"/> when the path is valid and unique.</returns>
    internal static ArtifactRenderDiagnostic? Check(string path, ISet<string> seen)
    {
        var reason = KitPath.Validate(path);
        if (reason is null)
        {
            try
            {
                PlannedArtifact.NormalizePath(path);
            }
            catch (InvalidArtifactRenderContract exception)
            {
                reason = exception.Message;
            }
        }

        if (reason is not null)
        {
            return DeclarativeOutputExpansion.Refuse(DeclarativeCodes.PathInvalid, $"The rendered path '{path}' is not a valid relative artifact path ({reason}).");
        }

        return seen.Add(path)
            ? null
            : DeclarativeOutputExpansion.Refuse(DeclarativeCodes.PathCollision, $"The rendered path '{path}' collides with another rendered path, ignoring case.");
    }
}
