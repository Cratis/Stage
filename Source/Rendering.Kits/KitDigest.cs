// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;

namespace Cratis.Stage.Rendering.Kits;

/// <summary>Computes the canonical digest of an uncompressed kit archive.</summary>
public static class KitDigest
{
    /// <summary>Computes lowercase hexadecimal SHA-256.</summary>
    /// <param name="archive">Canonical archive bytes, including the manifest.</param>
    /// <returns>A 64-character digest.</returns>
    public static string Compute(ImmutableArray<byte> archive) => Convert.ToHexStringLower(SHA256.HashData(archive.AsSpan()));
}
