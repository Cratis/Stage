// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.given;

/// <summary>
/// The full immutable identifier of a container: 64 lowercase hexadecimal characters.
/// </summary>
/// <param name="Value">The identifier.</param>
public sealed record GeneratedApplicationContainerId(string Value)
{
    const int Length = 64;

    /// <summary>
    /// Parses an identifier strictly: no trimming, no extra text, no abbreviation.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="id">The identifier when valid.</param>
    /// <returns>True when the text is exactly one full identifier.</returns>
    public static bool TryParse(string? text, out GeneratedApplicationContainerId id)
    {
        id = null!;
        if (text is not { Length: Length } || !text.All(IsLowerHex))
        {
            return false;
        }

        id = new(text);
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;

    static bool IsLowerHex(char character) => character is (>= '0' and <= '9') or (>= 'a' and <= 'f');
}
