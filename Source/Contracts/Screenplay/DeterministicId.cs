// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Produces stable <see cref="Guid"/> identifiers derived from a name, so that re-compiling the same Screenplay
/// source yields the same <see cref="EventModel"/> identity graph. Mirrors the SHA-256 derivation used by the
/// runtime when it registers projections with Chronicle.
/// </summary>
public static class DeterministicId
{
    /// <summary>
    /// Derives a deterministic <see cref="Guid"/> from the given value.
    /// </summary>
    /// <param name="value">The value to derive the identifier from — typically a fully-qualified name.</param>
    /// <returns>A stable <see cref="Guid"/> for the value.</returns>
    public static Guid From(string value) => new(SHA256.HashData(Encoding.UTF8.GetBytes(value))[..16]);
}
