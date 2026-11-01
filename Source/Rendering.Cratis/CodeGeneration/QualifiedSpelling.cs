// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration;

/// <summary>
/// Switches the renderers, for the current asynchronous flow, to the fully qualified spelling they produced before
/// imports were resolved. Specifications use it as the oracle the short-name output must bind exactly like.
/// </summary>
internal static class QualifiedSpelling
{
    static readonly AsyncLocal<bool> _active = new();

    /// <summary>
    /// Gets a value indicating whether the fully qualified spelling is active.
    /// </summary>
    public static bool IsActive => _active.Value;

    /// <summary>
    /// Activates the fully qualified spelling until the returned scope is disposed.
    /// </summary>
    /// <returns>The scope.</returns>
    public static IDisposable Begin()
    {
        var previous = _active.Value;
        _active.Value = true;
        return new Scope(previous);
    }

    sealed class Scope(bool previous) : IDisposable
    {
        public void Dispose() => _active.Value = previous;
    }
}
