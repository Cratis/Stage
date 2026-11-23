// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Scaffolding;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_the_application_user_interface.given;

/// <summary>
/// Computes a version-independent fingerprint of a plan: every artifact's path, content hash and sources, and every
/// diagnostic. The plan digest also covers the renderer version, which changes with every build; this does not.
/// Only the two exact current Screenplay scaffold pins are normalized to the 6.33.0 fixture baseline.
/// </summary>
public static class PlanFingerprint
{
    /// <summary>
    /// Computes the fingerprint.
    /// </summary>
    /// <param name="plan">The plan.</param>
    /// <returns>The lowercase hexadecimal SHA-256 of the canonical listing.</returns>
    public static string Of(ArtifactRenderPlan plan)
    {
        var listing = new StringBuilder();
        foreach (var artifact in plan.Artifacts.OrderBy(artifact => artifact.RelativePath, StringComparer.Ordinal))
        {
            listing.Append(artifact.RelativePath).Append('\0').Append(ArtifactHash(artifact)).Append('\0')
                .AppendJoin(',', artifact.Sources.Select(source => source.ToString()).Order(StringComparer.Ordinal)).Append('\n');
        }

        foreach (var diagnostic in plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code}\0{diagnostic.Severity}\0{diagnostic.Artifact}\0{diagnostic.Message}").Order(StringComparer.Ordinal))
        {
            listing.Append(diagnostic).Append('\n');
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(listing.ToString())));
    }

    static string ArtifactHash(PlannedArtifact artifact)
    {
        if (!artifact.RelativePath.EndsWith("Directory.Packages.props", StringComparison.Ordinal)) return artifact.Sha256;
        var text = Encoding.UTF8.GetString(artifact.Bytes.AsSpan())
            .Replace($"Include=\"Cratis.Screenplay\" Version=\"{ScaffoldPackageVersions.Screenplay}\"", "Include=\"Cratis.Screenplay\" Version=\"6.33.0\"", StringComparison.Ordinal)
            .Replace($"Include=\"Cratis.Screenplay.Matching\" Version=\"{ScaffoldPackageVersions.ScreenplayMatching}\"", "Include=\"Cratis.Screenplay.Matching\" Version=\"6.33.0\"", StringComparison.Ordinal);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }
}
