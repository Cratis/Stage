// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// The exception that is thrown when a legacy path would discard a generation strategy.
/// </summary>
/// <param name="location">The authored source location.</param>
public sealed class UnsupportedGenerationStrategy(SourceLocation location) : Exception(
    $"{DiagnosticCode}: Generation strategies at {location} cannot be preserved by Stage's legacy syntax paths.")
{
    /// <summary>
    /// The stable diagnostic code for unsupported generation strategies.
    /// </summary>
    public const string DiagnosticCode = "STAGE-ESM-028";

    /// <summary>
    /// Gets the authored source location.
    /// </summary>
    public SourceLocation Location { get; } = location;
}
