// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// Defines whether a target may render around explicitly supported artifact refusals.
/// </summary>
public enum ArtifactRefusalMode
{
    /// <summary>
    /// Refuse the entire plan when any artifact is refused.
    /// </summary>
    Refuse = 0,

    /// <summary>
    /// Render the remaining artifacts when the target can safely omit every refused artifact.
    /// </summary>
    RenderAround = 1
}
