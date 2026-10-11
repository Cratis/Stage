// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// The exception that is thrown when a legacy path would discard public-event publication.
/// </summary>
/// <param name="location">The authored source location.</param>
public sealed class UnsupportedPublicEvents(SourceLocation location) : Exception(
    $"{DiagnosticCode}: Public events at {location} cannot be published by Stage's legacy syntax paths.")
{
    /// <summary>
    /// The stable diagnostic code for unsupported public-event publication.
    /// </summary>
    public const string DiagnosticCode = "STAGE-ESM-031";

    /// <summary>
    /// Gets the authored source location.
    /// </summary>
    public SourceLocation Location { get; } = location;
}
