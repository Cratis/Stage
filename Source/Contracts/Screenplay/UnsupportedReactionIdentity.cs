// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// The exception that is thrown when a reaction execution identity would be discarded by a legacy path.
/// </summary>
/// <param name="location">The authored source location.</param>
public sealed class UnsupportedReactionIdentity(SourceLocation location) : Exception(
    $"{DiagnosticCode}: Reaction execution identity at {location} cannot run on the legacy event model or renderer; Stage does not support runsAs yet.")
{
    /// <summary>
    /// The stable diagnostic code for unsupported reaction identity.
    /// </summary>
    public const string DiagnosticCode = "STAGE-ESM-034";

    /// <summary>
    /// Gets the authored source location.
    /// </summary>
    public SourceLocation Location { get; } = location;
}
