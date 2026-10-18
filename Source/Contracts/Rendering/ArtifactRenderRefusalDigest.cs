// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Contracts.Rendering;

static class ArtifactRenderRefusalDigest
{
    public static ImmutableArray<ArtifactRenderRefusal> Normalize(ImmutableArray<ArtifactRenderRefusal> refusals)
    {
        if (refusals.IsDefault || refusals.Any(refusal => refusal?.Artifact.IsSet != true ||
            string.IsNullOrWhiteSpace(refusal.Kind) || refusal.Codes.IsDefaultOrEmpty || refusal.Codes.Any(string.IsNullOrWhiteSpace)) ||
            refusals.Select(refusal => refusal.Artifact).Distinct().Count() != refusals.Length)
        {
            throw new InvalidArtifactRenderContract("Artifact refusals require unique set identities, kinds, and nonempty diagnostic codes.");
        }

        return [.. refusals.OrderBy(refusal => refusal.Artifact.ToString(), StringComparer.Ordinal)
            .Select(refusal => refusal with { Codes = [.. refusal.Codes.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)] })];
    }

    public static void Write(BinaryWriter writer, ImmutableArray<ArtifactRenderRefusal> refusals)
    {
        var normalized = Normalize(refusals);
        if (normalized.IsEmpty)
        {
            return;
        }

        writer.Write("refusals-v1");
        writer.Write(normalized.Length);
        foreach (var refusal in normalized)
        {
            writer.Write(refusal.Artifact.ToString());
            writer.Write(refusal.Kind);
            writer.Write(refusal.Codes.Length);
            foreach (var code in refusal.Codes) writer.Write(code);
        }
    }
}
