// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// Carries final headed artifacts or a blocking header refusal.
/// </summary>
/// <param name="Artifacts">The final artifacts, empty on refusal.</param>
/// <param name="Diagnostics">The header refusal diagnostics.</param>
public sealed record ArtifactFileHeaderResult(ImmutableArray<PlannedArtifact> Artifacts, ImmutableArray<ArtifactRenderDiagnostic> Diagnostics)
{
    /// <summary>
    /// Gets whether the header could not be safely applied.
    /// </summary>
    public bool Refused => !Diagnostics.IsEmpty;

    internal ImmutableDictionary<string, int> Prepended { get; init; } = [];

    internal ImmutableArray<ArtifactImplementationGap> Rebase(ImmutableArray<ArtifactImplementationGap> gaps)
    {
        if (Refused) return [];
        if (Prepended.IsEmpty || gaps.IsDefaultOrEmpty) return gaps;
        return [.. gaps.Select(Rebase)];
    }

    internal ImmutableArray<ArtifactRemoval> Retain(ImmutableArray<ArtifactRemoval> removals) => Refused ? [] : removals;

    ArtifactImplementationGap Rebase(ArtifactImplementationGap gap)
    {
        if (gap?.Stubs.IsDefaultOrEmpty != false) return gap!;
        return gap with { Stubs = [.. gap.Stubs.Select(Rebase)] };
    }

    ArtifactImplementationStub Rebase(ArtifactImplementationStub stub)
    {
        if (stub?.BodyLocator is null) return stub!;
        return Prepended.TryGetValue(PlannedArtifact.NormalizePath(stub.RelativePath), out var length)
            ? stub with { BodyLocator = stub.BodyLocator with { Start = stub.BodyLocator.Start + length } }
            : stub;
    }
}
