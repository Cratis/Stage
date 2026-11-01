// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Stage.Contracts.Rendering;

public sealed partial class ArtifactRenderPlan
{
    /// <summary>Gets the authenticated inline-input fingerprint, or null when no inline body was rendered.</summary>
    public string? InlineCodeFingerprint { get; private set; }

    /// <summary>Retains the renderer's fingerprint of the verified inline inputs independently of emitted artifact bytes.</summary>
    /// <param name="fingerprint">A lowercase SHA-256 fingerprint, or null to retain the existing provenance.</param>
    /// <returns>A new plan retaining all existing metadata and acceptance.</returns>
    public ArtifactRenderPlan WithInlineCodeFingerprint(string? fingerprint)
    {
        if (fingerprint is null) return ApplyAcceptance(Degradations, Strict, Inventory);
        if (fingerprint.Length != 64 || !fingerprint.All(char.IsAsciiHexDigitLower))
            throw new InvalidArtifactRenderContract("Inline input provenance requires a lowercase SHA-256 fingerprint.");
        var plan = ApplyAcceptance(Degradations, Strict, Inventory);
        plan.InlineCodeFingerprint = fingerprint;
        return plan;
    }

    string InlineInputDigest(string original)
    {
        if (InlineCodeFingerprint is null) return original;
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, new UTF8Encoding(false), leaveOpen: true);
        writer.Write("stage-inline-inputs-v1");
        writer.Write(original);
        writer.Write(InlineCodeFingerprint);
        writer.Flush();
        return Convert.ToHexStringLower(SHA256.HashData(stream.ToArray()));
    }
}
