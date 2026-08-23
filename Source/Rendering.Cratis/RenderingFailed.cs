// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// The exception thrown after a render operation has attempted all independent artifacts and one or more blocking
/// failures occurred.
/// </summary>
/// <param name="failures">The blocking failures collected during the operation.</param>
public sealed class RenderingFailed(IReadOnlyCollection<Exception> failures) : Exception(
    $"Rendering failed with {failures.Count} blocking failure(s). The target output is unsafe and incomplete; stale artifacts from earlier runs may remain.")
{
    /// <summary>
    /// Gets the blocking failures collected during the operation.
    /// </summary>
    public IReadOnlyList<Exception> Failures { get; } = [.. failures];
}
