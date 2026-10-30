// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// Describes whether a plan realizes all, some, or none of its requested artifacts.
/// </summary>
public enum ArtifactRenderOutcome
{
    /// <summary>
    /// The plan has no blocking diagnostics.
    /// </summary>
    Rendered = 0,

    /// <summary>
    /// Compatibility alias for Rendered.
    /// </summary>
    Complete = Rendered,

    /// <summary>
    /// The plan renders artifacts while explicitly listing omitted refusals.
    /// </summary>
    Degraded = 1,

    /// <summary>
    /// Compatibility alias for Degraded.
    /// </summary>
    Partial = Degraded,

    /// <summary>
    /// The plan could not render the requested output.
    /// </summary>
    Refused = 2
}
