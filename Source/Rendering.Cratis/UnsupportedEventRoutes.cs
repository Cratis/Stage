// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Stage.Contracts.Screenplay;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// The exception thrown when syntax rendering would discard an event route.
/// </summary>
/// <param name="location">The authored route location.</param>
public sealed class UnsupportedEventRoutes(SourceLocation location) : Exception(
    $"{DiagnosticCode}: Named event sources, streams and event-source routes at {location} are not supported by the legacy syntax renderer. " +
    "Use the semantic planner to render event-source routes.")
{
    /// <summary>
    /// The stable diagnostic code shared with semantic route admission.
    /// </summary>
    public const string DiagnosticCode = UnsupportedEventSourceRoutes.DiagnosticCode;

    /// <summary>
    /// Gets the authored route location.
    /// </summary>
    public SourceLocation Location { get; } = location;
}
