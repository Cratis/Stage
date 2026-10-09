// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The exception thrown when authored UI syntax has no faithful Scene translation.
/// </summary>
/// <param name="member">The unsupported syntax member.</param>
/// <param name="location">The authored source location.</param>
public sealed class UnsupportedUiSyntax(string member, SourceLocation location) : Exception(
    $"{DiagnosticCode}: '{member}' at {location} has no faithful Stage Scene translation. No scene was emitted.")
{
    /// <summary>
    /// The stable diagnostic code for unsupported UI syntax.
    /// </summary>
    public const string DiagnosticCode = "STAGE-SCENE-UI-001";

    /// <summary>
    /// Gets the unsupported syntax member.
    /// </summary>
    public string Member { get; } = member;

    /// <summary>
    /// Gets the authored source location.
    /// </summary>
    public SourceLocation Location { get; } = location;
}
