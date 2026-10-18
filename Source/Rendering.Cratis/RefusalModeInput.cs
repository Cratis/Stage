// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Defines the hashed v1 refusal policy input. Its absence means Refuse.
/// </summary>
public static class RefusalModeInput
{
    /// <summary>
    /// The stable profile input name.
    /// </summary>
    public const string Name = "cratis-render:refusals";

    /// <summary>
    /// The profile input format version.
    /// </summary>
    public const string Version = "1";

    /// <summary>
    /// Creates the exact UTF-8 mode name without a trailing newline.
    /// </summary>
    /// <param name="mode">The requested refusal mode.</param>
    /// <returns>The hashed profile input.</returns>
    /// <exception cref="InvalidArtifactRenderContract">The mode is unknown.</exception>
    public static ArtifactRenderInput Create(ArtifactRefusalMode mode)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new InvalidArtifactRenderContract("A refusal-mode input requires a known mode.");
        }

        return ArtifactRenderInput.Create(Name, Version, [.. Encoding.UTF8.GetBytes(mode.ToString())]);
    }

    /// <summary>
    /// Reads only canonical v1 refusal policy inputs.
    /// </summary>
    /// <param name="input">The versioned input.</param>
    /// <param name="mode">The mode, or Refuse when invalid.</param>
    /// <returns>Whether the input is a canonical refusal policy.</returns>
    public static bool TryRead(ArtifactRenderInput input, out ArtifactRefusalMode mode)
    {
        mode = ArtifactRefusalMode.Refuse;
        if (input is null || input.Name != Name || input.Version != Version)
        {
            return false;
        }
        if (input.Bytes.AsSpan().SequenceEqual("Refuse"u8))
        {
            return true;
        }
        if (input.Bytes.AsSpan().SequenceEqual("RenderAround"u8))
        {
            mode = ArtifactRefusalMode.RenderAround;
            return true;
        }

        return false;
    }
}
