// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// The exception that is thrown when a legacy path would discard foreign-event consumption.
/// </summary>
/// <param name="location">The authored source location.</param>
public sealed class UnsupportedForeignEvents(SourceLocation location) : Exception(
    $"{DiagnosticCode}: Foreign events and source-events captures at {location} cannot be consumed by Stage's legacy syntax paths.")
{
    /// <summary>
    /// The stable diagnostic code for unsupported foreign-event consumption.
    /// </summary>
    public const string DiagnosticCode = "STAGE-ESM-033";

    /// <summary>
    /// Gets the authored source location.
    /// </summary>
    public SourceLocation Location { get; } = location;
}
