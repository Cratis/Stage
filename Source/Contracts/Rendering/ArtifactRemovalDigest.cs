// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Contracts.Rendering;

static class ArtifactRemovalDigest
{
    public static ImmutableArray<ArtifactRemoval> Normalize(ImmutableArray<ArtifactRemoval> removals)
    {
        if (removals.IsDefault || removals.Any(removal => removal?.Model.IsSet != true || !IsSha256(removal.ExpectedSha256)))
        {
            throw new InvalidArtifactRenderContract("Required removals need a model identity and a lowercase SHA-256 of the expected content.");
        }

        var normalized = removals
            .Select(removal => removal with { RelativePath = PlannedArtifact.NormalizePath(removal.RelativePath) })
            .OrderBy(removal => removal.RelativePath, StringComparer.Ordinal)
            .ToImmutableArray();
        if (normalized.Select(removal => removal.RelativePath).Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalized.Length)
        {
            throw new InvalidArtifactRenderContract("Required removals contain paths that collide on a case-insensitive file system.");
        }

        return normalized;
    }

    public static ImmutableArray<ArtifactRemoval> Validate(
        ImmutableArray<ArtifactRemoval> removals,
        ImmutableArray<PlannedArtifact> artifacts,
        ImmutableArray<ArtifactRenderDiagnostic> diagnostics)
    {
        var normalized = Normalize(removals);
        if (normalized.IsEmpty)
        {
            return normalized;
        }

        if (diagnostics.Any(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error))
        {
            throw new InvalidArtifactRenderContract("A plan carrying errors cannot require removals.");
        }

        var paths = artifacts.Select(artifact => artifact.RelativePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (normalized.Any(removal => paths.Contains(removal.RelativePath)))
        {
            throw new InvalidArtifactRenderContract("A required removal cannot share a path with a planned artifact.");
        }

        var sources = artifacts.SelectMany(artifact => artifact.Sources).ToHashSet();
        if (normalized.Any(removal => !sources.Contains(removal.Model)))
        {
            throw new InvalidArtifactRenderContract("A required removal must be paired with a planned artifact realizing its model.");
        }

        return normalized;
    }

    public static void Write(BinaryWriter writer, ImmutableArray<ArtifactRemoval> removals)
    {
        var normalized = Normalize(removals);
        if (normalized.IsEmpty)
        {
            return;
        }

        writer.Write("removals-v1");
        writer.Write(normalized.Length);
        foreach (var removal in normalized)
        {
            writer.Write(removal.RelativePath);
            writer.Write(removal.ExpectedSha256);
            writer.Write(removal.Model.ToString());
        }
    }

    static bool IsSha256(string? value) =>
        value is { Length: 64 } && value.All(character => char.IsAsciiDigit(character) || character is >= 'a' and <= 'f');
}
