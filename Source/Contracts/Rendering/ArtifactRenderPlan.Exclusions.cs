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

    string ExclusionsDigest(string original) => ArtifactRenderExclusions.Digest(original, Exclusions);
}
