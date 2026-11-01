// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>Describes a semantic gap without carrying an implementation body.</summary>
/// <param name="Evidence">The original warning and its actionable remedy.</param>
/// <param name="PlaceholderKind">The semantic placeholder behavior.</param>
/// <param name="Intent">The modeled requirement, not a raw implementation body.</param>
/// <param name="ModelPath">The real portable model-relative source filename, or null when unavailable.</param>
public sealed record ArtifactRenderConstructGap(ArtifactRenderDegradation Evidence, ArtifactRenderPlaceholderKind PlaceholderKind, string Intent, string? ModelPath)
{
    /// <summary>Gets the same exact typed semantic address as the owning construct.</summary>
    public required SemanticAddress SemanticLocator { get; init; }

    /// <summary>Gets the causes of a 005 dependency gap. Independent 001–004 gaps have no references.</summary>
    public ImmutableArray<ArtifactRenderGapReference> Roots { get; init; } = [];
}
