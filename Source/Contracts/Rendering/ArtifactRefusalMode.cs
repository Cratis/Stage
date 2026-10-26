// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// Defines whether a target may render around explicitly supported artifact refusals.
/// </summary>
public enum ArtifactRefusalMode
{
    /// <summary>
    /// Refuse the entire plan when any runtime artifact is refused; specifications that cannot be rendered are omitted and reported in Refusals.
    /// </summary>
    Refuse = 0,

    /// <summary>
    /// Render the remaining runtime artifacts when the target can safely omit every refused runtime artifact;
    /// specifications that cannot be rendered are omitted and reported in Refusals.
    /// </summary>
    RenderAround = 1
}
