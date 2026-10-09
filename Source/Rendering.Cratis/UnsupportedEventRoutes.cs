// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// The exception thrown when syntax rendering would discard an event route.
/// </summary>
/// <param name="location">The authored route location.</param>
public sealed class UnsupportedEventRoutes(SourceLocation location) : Exception(
    $"{DiagnosticCode}: Event-source streams and event routes at {location} cannot be rendered faithfully. " +
    "Support is tracked at https://github.com/Cratis/Stage/issues/177.")
{
    /// <summary>
    /// The stable diagnostic code shared with semantic route admission.
    /// </summary>
    public const string DiagnosticCode = "STAGE-ESM-016";

    /// <summary>
    /// Gets the authored route location.
    /// </summary>
    public SourceLocation Location { get; } = location;
}
