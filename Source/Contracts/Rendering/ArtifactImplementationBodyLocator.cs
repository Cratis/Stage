// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// Locates only a stub's body interior, excluding braces, in LF-normalized artifact text.
/// Offsets count UTF-16 code units from zero; the end is exclusive.
/// </summary>
/// <param name="Start">The zero-based body interior offset.</param>
/// <param name="Length">The number of UTF-16 code units in the body interior.</param>
public sealed record ArtifactImplementationBodyLocator(int Start, int Length)
{
    internal void Validate()
    {
        if (Start < 0 || Length <= 0 || (long)Start + Length > int.MaxValue)
        {
            throw new InvalidArtifactRenderContract("An implementation body locator requires a nonnegative start and a positive, non-overflowing length.");
        }
    }
}
