// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Declarative;

/// <summary>
/// Represents a language's resolution of one file: ordered pieces, or an integrity refusal.
/// </summary>
/// <param name="Pieces">The ordered pieces; empty when refused.</param>
/// <param name="Refusal">The refusal, or <see langword="null"/> when resolved.</param>
public sealed record DeclarativeFileResolution(ImmutableArray<DeclarativePiece> Pieces, DeclarativeRefusal? Refusal)
{
    /// <summary>
    /// Creates a resolved file.
    /// </summary>
    /// <param name="pieces">The ordered pieces.</param>
    /// <returns>The resolution.</returns>
    public static DeclarativeFileResolution Resolved(ImmutableArray<DeclarativePiece> pieces) => new(pieces, null);

    /// <summary>
    /// Creates a refusal, for example for an ambiguous short name.
    /// </summary>
    /// <param name="code">The language-owned code.</param>
    /// <param name="message">The reason.</param>
    /// <returns>The resolution.</returns>
    public static DeclarativeFileResolution Refused(string code, string message) => new([], new(code, message));
}
