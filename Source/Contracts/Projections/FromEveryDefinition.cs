// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Projections;

/// <summary>
/// Represents property mappings applied for every event in the projection.
/// </summary>
/// <param name="Properties">The property mappings applied for every event.</param>
/// <param name="IncludeChildren">Whether to include event types from child projections.</param>
/// <param name="AutoMap">The auto-map setting.</param>
public record FromEveryDefinition(
    IReadOnlyList<PropertyMapping> Properties,
    bool IncludeChildren = false,
    ProjectionAutoMap AutoMap = ProjectionAutoMap.Inherit);
