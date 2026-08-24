// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// Represents one fully resolved, versioned renderer input such as a scaffold or template.
/// </summary>
public sealed class ArtifactRenderInput
{
    ArtifactRenderInput(string name, string version, ImmutableArray<byte> bytes, string sha256)
    {
        Name = name;
        Version = version;
        Bytes = bytes;
        Sha256 = sha256;
    }

    /// <summary>
    /// Gets the stable input name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the exact input version.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets the immutable input bytes.
    /// </summary>
    public ImmutableArray<byte> Bytes { get; }

    /// <summary>
    /// Gets the lowercase SHA-256 of <see cref="Bytes"/>.
    /// </summary>
    public string Sha256 { get; }

    /// <summary>
    /// Creates and hashes one fully resolved renderer input.
    /// </summary>
    /// <param name="name">The stable input name.</param>
    /// <param name="version">The exact input version.</param>
    /// <param name="bytes">The immutable input bytes.</param>
    /// <returns>The validated input.</returns>
    public static ArtifactRenderInput Create(string name, string version, ImmutableArray<byte> bytes)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(version) || bytes.IsDefault)
        {
            throw new InvalidArtifactRenderContract("A renderer input requires a name, version, and non-default bytes.");
        }

        return new(name, version, bytes, Convert.ToHexString(SHA256.HashData(bytes.AsSpan())).ToLowerInvariant());
    }
}

/// <summary>
/// Represents the immutable, fully resolved target and renderer profile used for planning.
/// </summary>
public sealed class ArtifactRenderProfile
{
    ArtifactRenderProfile(
        string target,
        string targetVersion,
        string renderer,
        string rendererVersion,
        ImmutableArray<ArtifactRenderInput> inputs)
    {
        Target = target;
        TargetVersion = targetVersion;
        Renderer = renderer;
        RendererVersion = rendererVersion;
        Inputs = inputs;
    }

    /// <summary>
    /// Gets the target identity, such as <c>cratis</c>.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Gets the exact target profile version.
    /// </summary>
    public string TargetVersion { get; }

    /// <summary>
    /// Gets the renderer identity.
    /// </summary>
    public string Renderer { get; }

    /// <summary>
    /// Gets the exact renderer version.
    /// </summary>
    public string RendererVersion { get; }

    /// <summary>
    /// Gets fully resolved renderer inputs ordered by name and version.
    /// </summary>
    public ImmutableArray<ArtifactRenderInput> Inputs { get; }

    /// <summary>
    /// Creates a validated immutable render profile.
    /// </summary>
    /// <param name="target">The target identity.</param>
    /// <param name="targetVersion">The exact target profile version.</param>
    /// <param name="renderer">The renderer identity.</param>
    /// <param name="rendererVersion">The exact renderer version.</param>
    /// <param name="inputs">The fully resolved inputs.</param>
    /// <returns>The render profile.</returns>
    public static ArtifactRenderProfile Create(
        string target,
        string targetVersion,
        string renderer,
        string rendererVersion,
        ImmutableArray<ArtifactRenderInput> inputs)
    {
        if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(targetVersion) ||
            string.IsNullOrWhiteSpace(renderer) || string.IsNullOrWhiteSpace(rendererVersion) || inputs.IsDefault ||
            inputs.Any(_ => _ is null))
        {
            throw new InvalidArtifactRenderContract("A render profile requires target and renderer identities, exact versions, and non-default inputs.");
        }

        var duplicates = inputs.GroupBy(_ => (_.Name, _.Version)).Any(_ => _.Count() > 1);
        if (duplicates)
        {
            throw new InvalidArtifactRenderContract("A render profile contains a duplicate input name and version.");
        }

        return new(
            target,
            targetVersion,
            renderer,
            rendererVersion,
            [.. inputs.OrderBy(_ => _.Name, StringComparer.Ordinal).ThenBy(_ => _.Version, StringComparer.Ordinal)]);
    }
}
