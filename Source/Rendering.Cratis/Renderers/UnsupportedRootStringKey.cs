// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// The exception that is thrown when a root string key profile cannot be rendered faithfully.
/// </summary>
/// <param name="slicePath">The selected slice path.</param>
/// <param name="projectionName">The authored projection name.</param>
/// <param name="readModel">The authored read model name, or projection name when omitted.</param>
/// <param name="eventName">The relevant effective root event name.</param>
/// <param name="reason">The unsupported shape.</param>
/// <param name="location">The original relevant syntax location, without normalization.</param>
public sealed class UnsupportedRootStringKey(
    string slicePath,
    string projectionName,
    string readModel,
    string eventName,
    UnsupportedRootStringKeyReason reason,
    SourceLocation location) : Exception(
        $"{DiagnosticCode}: Root string key profile rejected ({reason}) at {location} on event '{eventName}' " +
        $"of projection '{projectionName}' for read model '{readModel}' in slice '{slicePath}'. " +
        "Only nonempty strings in the target value-expression grammar, without separate record identity, root parent " +
        "keys, or incompatible identifying query types, can be preserved. No affected slice artifact was emitted.")
{
    /// <summary>
    /// The stable diagnostic code for unsupported root string key profiles.
    /// </summary>
    public const string DiagnosticCode = "STAGE-CRATIS-KEY-002";

    /// <summary>
    /// Gets the selected slice path.
    /// </summary>
    public string SlicePath { get; } = slicePath;

    /// <summary>
    /// Gets the authored projection name.
    /// </summary>
    public string ProjectionName { get; } = projectionName;

    /// <summary>
    /// Gets the authored read model name, or projection name when omitted.
    /// </summary>
    public string ReadModel { get; } = readModel;

    /// <summary>
    /// Gets the relevant effective root event name.
    /// </summary>
    public string EventName { get; } = eventName;

    /// <summary>
    /// Gets the unsupported shape.
    /// </summary>
    public UnsupportedRootStringKeyReason Reason { get; } = reason;

    /// <summary>
    /// Gets the original relevant syntax location, without normalization.
    /// </summary>
    public SourceLocation Location { get; } = location;
}
