// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>Provides explicit public spellings, independent of enum compatibility aliases.</summary>
public static class ArtifactRenderOutcomeExtensions
{
    /// <summary>Gets the canonical prerelease outcome name. Do not serialize aliases using enum ToString().</summary>
    /// <param name="outcome">The known render outcome.</param>
    /// <returns>Rendered, Degraded or Refused.</returns>
    /// <exception cref="InvalidArtifactRenderContract">The numeric outcome is unknown.</exception>
    public static string ToCanonicalName(this ArtifactRenderOutcome outcome) => outcome switch
    {
        ArtifactRenderOutcome.Rendered => "Rendered",
        ArtifactRenderOutcome.Degraded => "Degraded",
        ArtifactRenderOutcome.Refused => "Refused",
        _ => throw new InvalidArtifactRenderContract("Unknown artifact render outcome.")
    };
}
