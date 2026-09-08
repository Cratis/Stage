// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host;

/// <summary>
/// The exception that is thrown when modeled HTTP operations cannot be admitted without ambiguous ownership.
/// </summary>
/// <param name="method">The HTTP method of the conflicting route.</param>
/// <param name="path">The normalized path of the conflicting route.</param>
/// <param name="operations">Descriptions containing operation kinds, slice IDs, and qualified artifacts.</param>
public sealed class AmbiguousStageHttpSurface(string method, string path, IEnumerable<string> operations)
    : Exception($"Ambiguous Stage HTTP surface at {method} {path}: {string.Join("; ", operations.Order(StringComparer.Ordinal))}")
{
    /// <summary>
    /// Gets the conflicting HTTP method.
    /// </summary>
    public string Method { get; } = method;

    /// <summary>
    /// Gets the normalized conflicting path.
    /// </summary>
    public string Path { get; } = path;
}
