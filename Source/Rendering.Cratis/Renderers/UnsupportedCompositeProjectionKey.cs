// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// The exception thrown when an effective root subscription's composite key cannot be preserved by legacy rendering.
/// </summary>
/// <param name="slicePath">The selected slice path.</param>
/// <param name="projectionName">The authored projection name.</param>
/// <param name="readModel">The authored read model name, or projection name when omitted.</param>
/// <param name="eventName">The selected subscription's authored event name.</param>
/// <param name="compositeType">The authored composite key type name.</param>
/// <param name="location">The original composite key syntax location, without path normalization.</param>
public sealed class UnsupportedCompositeProjectionKey(
    string slicePath,
    string projectionName,
    string readModel,
    string eventName,
    string compositeType,
    SourceLocation location) : Exception(
        $"{DiagnosticCode}: Composite key '{compositeType}' at {location} on root event '{eventName}' " +
        $"of projection '{projectionName}' for read model '{readModel}' in slice '{slicePath}' cannot be rendered " +
        "by legacy model-bound FromEvent attributes. Omitting it would route on the event source id and lose the " +
        "authored composite identity. No affected slice artifact was emitted.")
{
    /// <summary>
    /// The stable diagnostic code for unsupported effective root composite projection keys.
    /// </summary>
    public const string DiagnosticCode = "STAGE-CRATIS-KEY-001";

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
    /// Gets the selected subscription's authored event name.
    /// </summary>
    public string EventName { get; } = eventName;

    /// <summary>
    /// Gets the authored composite key type name.
    /// </summary>
    public string CompositeType { get; } = compositeType;

    /// <summary>
    /// Gets the original composite key syntax location, without path normalization.
    /// </summary>
    public SourceLocation Location { get; } = location;
}
