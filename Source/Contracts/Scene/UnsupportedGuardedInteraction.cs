// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The exception thrown when ordered interaction alternatives cannot be represented by the pinned Scene contract.
/// </summary>
/// <param name="location">The authored binding location.</param>
public sealed class UnsupportedGuardedInteraction(SourceLocation location) : Exception(
    $"{DiagnosticCode}: Guarded interaction at {location} cannot be rendered faithfully. No binding was emitted. " +
    "Support is tracked at https://github.com/Cratis/Stage/issues/209 and https://github.com/Cratis/Scene/issues/68.")
{
    /// <summary>
    /// The stable diagnostic code for unsupported guarded interactions.
    /// </summary>
    public const string DiagnosticCode = "STAGE-SCENE-INTERACTION-001";

    /// <summary>
    /// Gets the authored binding location.
    /// </summary>
    public SourceLocation Location { get; } = location;
}
