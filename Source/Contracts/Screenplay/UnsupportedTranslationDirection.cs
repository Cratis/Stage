// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// The exception that is thrown when a legacy path would discard a translation direction.
/// </summary>
/// <param name="location">The authored source location.</param>
/// <param name="direction">The authored translation direction.</param>
public sealed class UnsupportedTranslationDirection(SourceLocation location, string direction) : Exception(
    $"{DiagnosticCode}: {direction} translation at {location} cannot be executed by Stage's legacy syntax paths.")
{
    /// <summary>
    /// The stable diagnostic code for unsupported translation.
    /// </summary>
    public const string DiagnosticCode = "STAGE-ESM-024";

    /// <summary>
    /// Gets the authored source location.
    /// </summary>
    public SourceLocation Location { get; } = location;
}
