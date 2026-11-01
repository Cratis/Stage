// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>Defines canonical, hashed strict acceptance. Absence is false and does not change legacy profile bytes.</summary>
public static class StrictAcceptanceInput
{
    /// <summary>The stable profile input name.</summary>
    public const string Name = "stage:strict";

    /// <summary>The exact input format version.</summary>
    public const string Version = "1";

    /// <summary>Creates canonical UTF-8 true or false, without a trailing newline.</summary>
    /// <param name="strict">Whether degradation fails acceptance.</param>
    /// <returns>The hashed acceptance input.</returns>
    public static ArtifactRenderInput Create(bool strict) => ArtifactRenderInput.Create(Name, Version, strict ? [.. "true"u8] : [.. "false"u8]);

    /// <summary>Reads only canonical v1 acceptance inputs.</summary>
    /// <param name="input">The versioned input.</param>
    /// <param name="strict">The acceptance setting, or false when invalid.</param>
    /// <returns>Whether the input is canonical.</returns>
    public static bool TryRead(ArtifactRenderInput input, out bool strict)
    {
        strict = false;
        if (input is null) return false;
        if (input.Name != Name) return false;
        if (input.Version != Version) return false;
        if (input.Bytes.AsSpan().SequenceEqual("false"u8)) return true;
        if (!input.Bytes.AsSpan().SequenceEqual("true"u8)) return false;
        strict = true;
        return true;
    }
}
