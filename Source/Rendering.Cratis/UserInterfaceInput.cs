// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Defines the hashed v1 user interface input: the slices, beyond a plan's own scope, whose generated frontend
/// proxies are present in the application. Its presence makes a scoped plan emit the application's shared UI
/// (the Scene composition, its bindings and the Stage runtime module) composed over exactly those slices and the
/// plan's own; its absence keeps scoped plans free of shared UI.
/// </summary>
public static class UserInterfaceInput
{
    /// <summary>
    /// The stable profile input name.
    /// </summary>
    public const string Name = "cratis-render:user-interface";

    /// <summary>
    /// The profile input format version.
    /// </summary>
    public const string Version = "1";

    /// <summary>
    /// Creates the canonical JSON array of distinct slice identities in ordinal order.
    /// </summary>
    /// <param name="slices">The semantic slice identities; may be empty, which means only the plan's own scope.</param>
    /// <returns>The hashed profile input.</returns>
    /// <exception cref="InvalidArtifactRenderContract">An identity is null or blank.</exception>
    public static ArtifactRenderInput Create(IEnumerable<string> slices)
    {
        ArgumentNullException.ThrowIfNull(slices);
        var identities = slices.ToArray();
        if (identities.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidArtifactRenderContract("A user interface input requires non-blank slice identities.");
        }

        return ArtifactRenderInput.Create(Name, Version, [.. JsonSerializer.SerializeToUtf8Bytes(identities.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray())]);
    }

    /// <summary>
    /// Reads the slice identities carried by a profile.
    /// </summary>
    /// <param name="profile">The render profile.</param>
    /// <param name="slices">The slice identities, or null when the profile carries no user interface input.</param>
    /// <returns>Whether the profile carries at most one canonical user interface input.</returns>
    public static bool TryRead(ArtifactRenderProfile profile, out ImmutableArray<string>? slices)
    {
        ArgumentNullException.ThrowIfNull(profile);
        slices = null;
        var inputs = profile.Inputs.Where(input => input.Name == Name).ToArray();
        if (inputs.Length == 0) return true;
        if (inputs.Length > 1 || inputs[0].Version != Version) return false;

        try
        {
            var read = JsonSerializer.Deserialize<string[]>(new UTF8Encoding(false, true).GetString(inputs[0].Bytes.AsSpan()));
            if (read?.Any(string.IsNullOrWhiteSpace) != false) return false;
            if (!Create(read).Bytes.SequenceEqual(inputs[0].Bytes)) return false;
            slices = [.. read];
            return true;
        }
        catch (Exception exception) when (exception is JsonException or DecoderFallbackException)
        {
            return false;
        }
    }

    /// <summary>
    /// Adds the input to a profile.
    /// </summary>
    /// <param name="profile">A profile that does not already carry a user interface input.</param>
    /// <param name="slices">The slice identities.</param>
    /// <returns>The profile carrying the input.</returns>
    internal static ArtifactRenderProfile With(ArtifactRenderProfile profile, IEnumerable<string> slices) =>
        ArtifactRenderProfile.Create(
            profile.Target,
            profile.TargetVersion,
            profile.Renderer,
            profile.RendererVersion,
            [.. profile.Inputs, Create(slices)]);
}
