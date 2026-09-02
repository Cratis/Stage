// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Identifies which of the two system-wide projection blocks a mapping belongs to — the pair share a shape but
/// not an attribute, and their attributes take their arguments in opposite order, so the two are never inferred.
/// </summary>
public enum GlobalMappingScope
{
    /// <summary>An <c>all</c> block — every event type in the system, rendered as <c>[FromAll]</c>.</summary>
    All,

    /// <summary>An <c>every</c> block — every event the projection subscribes to, rendered as <c>[FromEvery]</c>.</summary>
    Every
}
