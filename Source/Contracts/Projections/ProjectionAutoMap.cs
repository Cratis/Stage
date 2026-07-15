// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Projections;

/// <summary>
/// Represents the auto-map setting for a projection or a nested part of it.
/// </summary>
public enum ProjectionAutoMap
{
    /// <summary>Inherit the auto-map setting from the parent.</summary>
    Inherit = 0,

    /// <summary>Disable auto-mapping.</summary>
    Disabled = 1,

    /// <summary>Enable auto-mapping.</summary>
    Enabled = 2,
}
