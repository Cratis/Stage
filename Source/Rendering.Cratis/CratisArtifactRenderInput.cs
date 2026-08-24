// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Creates fully resolved scaffold inputs consumed by the pure Cratis artifact planner.
/// </summary>
public static class CratisArtifactRenderInput
{
    const string BinaryPrefix = "cratis-scaffold:binary:";
    const string TextPrefix = "cratis-scaffold:text:";

    /// <summary>
    /// Creates one normalized UTF-8 text scaffold input.
    /// </summary>
    /// <param name="relativePath">The target-relative artifact path.</param>
    /// <param name="version">The exact scaffold or template version.</param>
    /// <param name="content">The resolved text.</param>
    /// <returns>The immutable renderer input.</returns>
    public static ArtifactRenderInput CreateText(string relativePath, string version, string content)
    {
        var artifact = PlannedArtifact.CreateText(relativePath, content);
        return ArtifactRenderInput.Create($"{TextPrefix}{artifact.RelativePath}", version, artifact.Bytes);
    }

    /// <summary>
    /// Creates one binary scaffold input.
    /// </summary>
    /// <param name="relativePath">The target-relative artifact path.</param>
    /// <param name="version">The exact scaffold or template version.</param>
    /// <param name="bytes">The resolved bytes.</param>
    /// <returns>The immutable renderer input.</returns>
    public static ArtifactRenderInput CreateBinary(string relativePath, string version, ImmutableArray<byte> bytes)
    {
        var artifact = PlannedArtifact.CreateBinary(relativePath, bytes);
        return ArtifactRenderInput.Create($"{BinaryPrefix}{artifact.RelativePath}", version, artifact.Bytes);
    }

    internal static bool TryCreateArtifact(ArtifactRenderInput input, out PlannedArtifact? artifact)
    {
        try
        {
            if (input.Name.StartsWith(TextPrefix, StringComparison.Ordinal))
            {
                var content = new UTF8Encoding(false, true).GetString(input.Bytes.AsSpan());
                artifact = PlannedArtifact.CreateText(input.Name[TextPrefix.Length..], content);
                return true;
            }

            if (input.Name.StartsWith(BinaryPrefix, StringComparison.Ordinal))
            {
                artifact = PlannedArtifact.CreateBinary(input.Name[BinaryPrefix.Length..], input.Bytes);
                return true;
            }
        }
        catch (Exception exception) when (exception is DecoderFallbackException or InvalidArtifactRenderContract)
        {
            artifact = null;
            return false;
        }

        artifact = null;
        return false;
    }
}
