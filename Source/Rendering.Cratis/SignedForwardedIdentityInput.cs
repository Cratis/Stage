// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Defines the hashed v1 input that opts an application into signed forwarded identity. Its absence means the
/// application trusts unsigned identity headers, as it always has.
/// </summary>
public static class SignedForwardedIdentityInput
{
    /// <summary>
    /// The stable profile input name.
    /// </summary>
    public const string Name = "cratis-render:signed-forwarded-identity";

    /// <summary>
    /// The profile input format version.
    /// </summary>
    public const string Version = "1";

    /// <summary>
    /// Creates the input that turns signed forwarded identity on.
    /// </summary>
    /// <returns>The hashed profile input.</returns>
    public static ArtifactRenderInput Create() => ArtifactRenderInput.Create(Name, Version, [.. "enabled"u8]);

    /// <summary>
    /// Reads whether a profile opts into signed forwarded identity.
    /// </summary>
    /// <param name="profile">The profile.</param>
    /// <param name="enabled">Whether the profile carries the canonical input; false when it is absent or invalid.</param>
    /// <returns>False when the profile carries the input more than once or in a form other than the canonical v1 one; otherwise true.</returns>
    public static bool TryRead(ArtifactRenderProfile profile, out bool enabled)
    {
        enabled = false;
        var inputs = profile.Inputs.Where(input => input.Name == Name).ToArray();
        if (inputs.Length == 0)
        {
            return true;
        }
        if (inputs.Length > 1 || !IsCanonical(inputs[0]))
        {
            return false;
        }

        enabled = true;
        return true;
    }

    internal static bool IsCarriedBy(ArtifactRenderProfile profile) => TryRead(profile, out var enabled) && enabled;

    internal static ArtifactRenderProfile With(ArtifactRenderProfile profile, bool enabled) =>
        enabled
            ? ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, [.. profile.Inputs, Create()])
            : profile;

    static bool IsCanonical(ArtifactRenderInput input) => input.Version == Version && input.Bytes.AsSpan().SequenceEqual("enabled"u8);
}
