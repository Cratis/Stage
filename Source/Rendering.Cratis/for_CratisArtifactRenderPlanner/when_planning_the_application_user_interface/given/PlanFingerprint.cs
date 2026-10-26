// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_the_application_user_interface.given;

/// <summary>
/// Computes a version-independent fingerprint of a plan: every artifact's path, content hash and sources, and every
/// diagnostic. The plan digest also covers the renderer version, which changes with every build; this does not.
/// </summary>
public static class PlanFingerprint
{
    /// <summary>
    /// Computes the fingerprint.
    /// </summary>
    /// <param name="plan">The plan.</param>
    /// <param name="beforeSchemaForwarding">Compare the recorded plan before the single generated schema-forwarding line was added.</param>
    /// <returns>The lowercase hexadecimal SHA-256 of the canonical listing.</returns>
    public static string Of(ArtifactRenderPlan plan, bool beforeSchemaForwarding = false)
    {
        var listing = new StringBuilder();
        foreach (var artifact in plan.Artifacts.OrderBy(artifact => artifact.RelativePath, StringComparer.Ordinal))
        {
            var hash = beforeSchemaForwarding && artifact.RelativePath == "src/stage.ts"
                ? WithoutSchemaForwarding(artifact).Sha256
                : artifact.Sha256;
            listing.Append(artifact.RelativePath).Append('\0').Append(hash).Append('\0')
                .AppendJoin(',', artifact.Sources.Select(source => source.ToString()).Order(StringComparer.Ordinal)).Append('\n');
        }

        foreach (var diagnostic in plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code}\0{diagnostic.Severity}\0{diagnostic.Artifact}\0{diagnostic.Message}").Order(StringComparer.Ordinal))
        {
            listing.Append(diagnostic).Append('\n');
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(listing.ToString())));
    }

    /// <summary>Removes only the intended #820 metadata line for comparison with recorded pre-change bytes.</summary>
    /// <param name="artifact">The generated Stage module.</param>
    /// <returns>The module before schema forwarding.</returns>
    public static PlannedArtifact WithoutSchemaForwarding(PlannedArtifact artifact) => PlannedArtifact.CreateText(
        artifact.RelativePath,
        Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Replace("        commandSchemas: (scene as unknown as StageSceneApplication).commandSchemas,\n", string.Empty, StringComparison.Ordinal));
}
