// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering;

public sealed partial class ArtifactRenderPlan
{
    /// <summary>
    /// Gets caller-supplied compiler exclusions in ordinal kind/address order, with canonical diagnostic and dependency order.
    /// Excluded declarations have no executable leaf identity; their evidence belongs to retained parents, not inventory owners.
    /// </summary>
    public ImmutableArray<SemanticExclusion> Exclusions { get; private init; } = [];

    ImmutableArray<ArtifactRenderDegradation> ExclusionEvidence { get; init; } = [];

    static (ImmutableArray<SemanticExclusion> Exclusions, ImmutableArray<ArtifactRenderDegradation> Evidence) PrepareExclusions(
        ArtifactRenderRequest request,
        ref ImmutableArray<ArtifactRenderDiagnostic> diagnostics)
    {
        var exclusions = ArtifactRenderExclusions.Normalize(request);
        var evidence = ArtifactRenderExclusions.Evidence(exclusions, request.Model.Application);
        var original = diagnostics;
        diagnostics = [.. original, .. evidence.Select(entry => entry.Diagnostic).Where(diagnostic => !original.Contains(diagnostic))];
        return (exclusions, evidence);
    }

    string ExclusionsDigest(string original) => ArtifactRenderExclusions.Digest(original, Exclusions);
}
