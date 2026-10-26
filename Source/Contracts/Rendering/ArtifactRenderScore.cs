// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>Integer weighted completeness, not a deployment-readiness threshold.</summary>
/// <param name="Counts">The semantic owner counts.</param>
/// <param name="EarnedWeight">The weight earned by Rendered owners only.</param>
/// <param name="TotalWeight">The possible weight of all selected owners.</param>
public sealed record ArtifactRenderScore(ArtifactRenderCounts Counts, long EarnedWeight, long TotalWeight)
{
    /// <summary>Gets floor(10000 * earned / possible), or null when the denominator is zero.</summary>
    public int? BasisPoints => TotalWeight == 0 ? null : (int)(10000 * EarnedWeight / TotalWeight);

    internal static ArtifactRenderScore For(IEnumerable<ArtifactRenderConstruct> constructs)
    {
        var owners = constructs.ToArray();
        return new(
            new(owners.Count(owner => owner.Outcome == ArtifactRenderOutcome.Rendered), owners.Count(owner => owner.Outcome == ArtifactRenderOutcome.Degraded), owners.Count(owner => owner.Outcome == ArtifactRenderOutcome.Refused)),
            owners.Where(owner => owner.Outcome == ArtifactRenderOutcome.Rendered).Sum(owner => (long)ArtifactRenderConstructWeights.For(owner.Kind)),
            owners.Sum(owner => (long)ArtifactRenderConstructWeights.For(owner.Kind)));
    }
}
