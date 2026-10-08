// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The exception thrown when a guarded screen action cannot be rendered faithfully by the pinned Scene packages.
/// </summary>
/// <param name="label">The authored action label.</param>
/// <param name="location">The authored action's source location.</param>
public sealed class UnsupportedGuardedScreenAction(string label, SourceLocation location) : Exception(
    $"{DiagnosticCode}: Guarded screen action '{label}' at {location} cannot be rendered by the pinned Scene packages. " +
    "Scene needs selected-item binding and ordered first-match command alternatives with nonmatching null or missing fields, " +
    "hidden or executable fallbacks, and hiding when no item is selected. No action was emitted.")
{
    /// <summary>
    /// The stable diagnostic code for guarded screen actions requiring Scene renderer support.
    /// </summary>
    public const string DiagnosticCode = "STAGE-SCENE-ACTION-001";

    /// <summary>
    /// Gets the authored action label.
    /// </summary>
    public string Label { get; } = label;

    /// <summary>
    /// Gets the authored action's source location, including its file path when known.
    /// </summary>
    public SourceLocation Location { get; } = location;
}
