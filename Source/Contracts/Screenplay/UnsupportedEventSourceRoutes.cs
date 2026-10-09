// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// The exception that is thrown when event-source routes would be discarded by the legacy event model.
/// </summary>
/// <param name="location">The authored source location.</param>
public sealed class UnsupportedEventSourceRoutes(SourceLocation location) : Exception(
    $"{DiagnosticCode}: Named event sources, streams and event-source routes at {location} cannot run on the legacy event model (eventmodel Host engine, structural SpecRunner). Routes run only on the semantic engine.")
{
    /// <summary>
    /// The stable diagnostic code for unsupported event-source routes.
    /// </summary>
    public const string DiagnosticCode = "STAGE-ESM-030";

    /// <summary>
    /// Gets the authored source location.
    /// </summary>
    public SourceLocation Location { get; } = location;
}
