// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>A caller-supplied semantic leaf in the selected inventory. Files, fields and containers are not inferred as leaves.</summary>
/// <param name="Identity">The stable semantic owner identity.</param>
/// <param name="Kind">The leaf category, independent of its graph eligibility.</param>
/// <param name="ModelPath">The real portable model-relative source filename, or null when unavailable.</param>
/// <param name="Outcome">The realization result, independent of strict acceptance.</param>
public sealed record ArtifactRenderConstruct(SemanticId Identity, ArtifactRenderConstructKind Kind, string? ModelPath, ArtifactRenderOutcome Outcome)
{
    /// <summary>Gets the exact typed semantic address in parent-to-child order, independent of source-file availability.</summary>
    public required SemanticAddress SemanticLocator { get; init; }

    /// <summary>Gets every independent or dependent degradation on this owner.</summary>
    public ImmutableArray<ArtifactRenderConstructGap> Degradations { get; init; } = [];

    /// <summary>Gets the recorded specification omission, if this owner was deliberately omitted from a partial plan.</summary>
    public ArtifactRenderRefusal? Omission { get; init; }

    /// <summary>Gets original errors or recorded omission warnings. These must also occur in the plan's original diagnostics.</summary>
    public ImmutableArray<ArtifactRenderDiagnostic> RefusalReasons { get; init; } = [];
}
