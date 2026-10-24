// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Defines the hashed v1 file header input: the plain-text lines every generated code file starts with.
/// Its absence means generated files carry no header at all.
/// </summary>
public static class FileHeaderInput
{
    /// <summary>
    /// The stable profile input name.
    /// </summary>
    public const string Name = "cratis-render:file-header";

    /// <summary>
    /// The profile input format version.
    /// </summary>
    public const string Version = "1";

    /// <summary>
    /// Gets whether every line is plain text that fits on one line.
    /// </summary>
    /// <param name="lines">The header lines.</param>
    /// <returns>True when no line is null or contains a line break.</returns>
    public static bool IsValid(ImmutableArray<string> lines) =>
        lines.IsDefault || lines.All(line => line?.Contains('\n') == false && !line.Contains('\r'));

    /// <summary>
    /// Creates the canonical JSON array of header lines.
    /// </summary>
    /// <param name="lines">One or more plain-text lines without comment markers.</param>
    /// <returns>The hashed profile input.</returns>
    /// <exception cref="InvalidArtifactRenderContract">The header is empty or a line is null or spans lines.</exception>
    public static ArtifactRenderInput Create(ImmutableArray<string> lines)
    {
        if (lines.IsDefaultOrEmpty || !IsValid(lines))
        {
            throw new InvalidArtifactRenderContract("A file header input requires one or more single-line, non-null lines.");
        }

        return ArtifactRenderInput.Create(Name, Version, [.. JsonSerializer.SerializeToUtf8Bytes(lines.ToArray())]);
    }

    /// <summary>
    /// Reads the header lines carried by a profile.
    /// </summary>
    /// <param name="profile">The render profile.</param>
    /// <param name="lines">The header lines, or an empty list when the profile carries none.</param>
    /// <returns>Whether the profile carries at most one canonical file header input.</returns>
    public static bool TryRead(ArtifactRenderProfile profile, out ImmutableArray<string> lines)
    {
        lines = [];
        var inputs = profile.Inputs.Where(input => input.Name == Name).ToArray();
        if (inputs.Length == 0) return true;
        if (inputs.Length > 1 || inputs[0].Version != Version) return false;

        try
        {
            var read = JsonSerializer.Deserialize<string[]>(new UTF8Encoding(false, true).GetString(inputs[0].Bytes.AsSpan()));
            if (read is null || read.Length == 0 || !IsValid([.. read])) return false;
            var canonical = Create([.. read]);
            if (!canonical.Bytes.SequenceEqual(inputs[0].Bytes)) return false;
            lines = [.. read];
            return true;
        }
        catch (Exception exception) when (exception is JsonException or DecoderFallbackException)
        {
            return false;
        }
    }

    /// <summary>
    /// Adds the header to a profile, or returns the profile unchanged for an empty header.
    /// </summary>
    /// <param name="profile">A profile that does not already carry a file header.</param>
    /// <param name="lines">The header lines.</param>
    /// <returns>The profile carrying the header.</returns>
    internal static ArtifactRenderProfile With(ArtifactRenderProfile profile, ImmutableArray<string> lines) =>
        lines.IsDefaultOrEmpty ? profile : ArtifactRenderProfile.Create(
            profile.Target,
            profile.TargetVersion,
            profile.Renderer,
            profile.RendererVersion,
            [.. profile.Inputs, Create(lines)]);
}
