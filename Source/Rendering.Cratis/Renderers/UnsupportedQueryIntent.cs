// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// The exception thrown when legacy query rendering would discard authored intent.
/// </summary>
/// <param name="queryName">The authored query name.</param>
/// <param name="readModel">The authored return type name.</param>
/// <param name="location">The relevant authored source location, without path normalization.</param>
/// <param name="reason">The unsupported intent.</param>
/// <param name="slicePath">The selected slice path, when known.</param>
public sealed class UnsupportedQueryIntent(
    string queryName,
    string readModel,
    SourceLocation location,
    UnsupportedQueryIntentReason reason,
    string? slicePath = null) : Exception(
        $"{DiagnosticCode}: Query '{queryName}' returning '{readModel}' at {location}" +
        (slicePath is null ? string.Empty : $" in slice '{slicePath}'") +
        $" cannot be rendered faithfully ({reason}). No query method was emitted.")
{
    /// <summary>
    /// The stable diagnostic code for unsupported legacy query intent.
    /// </summary>
    public const string DiagnosticCode = "STAGE-CRATIS-QUERY-001";

    /// <summary>
    /// Gets the authored query name.
    /// </summary>
    public string QueryName { get; } = queryName;

    /// <summary>
    /// Gets the authored return type name.
    /// </summary>
    public string ReadModel { get; } = readModel;

    /// <summary>
    /// Gets the first filter's location, otherwise the file or code location for an unambiguous performer,
    /// or the performer declaration's location when neither or both are present.
    /// </summary>
    public SourceLocation Location { get; } = location;

    /// <summary>
    /// Gets the unsupported intent.
    /// </summary>
    public UnsupportedQueryIntentReason Reason { get; } = reason;

    /// <summary>
    /// Gets the selected slice path, or null for a direct query-renderer call.
    /// </summary>
    public string? SlicePath { get; } = slicePath;
}
