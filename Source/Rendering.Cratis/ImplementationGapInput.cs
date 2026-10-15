// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Defines the hashed v1 implementation-gap policy input. Its absence means Refuse.
/// Profile integration and stub admission are separate target capabilities.
/// </summary>
public static class ImplementationGapInput
{
    /// <summary>
    /// The stable profile input name.
    /// </summary>
    public const string Name = "cratis-implementation:gaps";

    /// <summary>
    /// The profile input format version.
    /// </summary>
    public const string Version = "1";

    /// <summary>
    /// Creates the policy input using the exact UTF-8 mode name without a trailing newline.
    /// </summary>
    /// <param name="mode">The requested gap mode.</param>
    /// <returns>The fully resolved, hashed input.</returns>
    /// <exception cref="InvalidArtifactRenderContract">The mode is unknown.</exception>
    public static ArtifactRenderInput Create(ImplementationGapMode mode)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new InvalidArtifactRenderContract("An implementation-gap input requires a known mode.");
        }

        return ArtifactRenderInput.Create(Name, Version, [.. Encoding.UTF8.GetBytes(mode.ToString())]);
    }

    /// <summary>
    /// Reads a v1 policy input without accepting unknown versions, names, or noncanonical bytes.
    /// </summary>
    /// <param name="input">The versioned input.</param>
    /// <param name="mode">The decoded mode, or Refuse if the input is invalid.</param>
    /// <returns>Whether the input is a well-formed v1 implementation-gap policy.</returns>
    public static bool TryRead(ArtifactRenderInput input, out ImplementationGapMode mode)
    {
        mode = ImplementationGapMode.Refuse;
        if (input is null || input.Name != Name || input.Version != Version)
        {
            return false;
        }

        if (input.Bytes.AsSpan().SequenceEqual("Refuse"u8))
        {
            return true;
        }
        if (input.Bytes.AsSpan().SequenceEqual("Stub"u8))
        {
            mode = ImplementationGapMode.Stub;
            return true;
        }

        return false;
    }
}
