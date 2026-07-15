// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Projections;

/// <summary>
/// Represents a children projection nested within a parent projection, keyed by event type name.
/// </summary>
/// <param name="IdentifiedBy">The property path that uniquely identifies each child instance.</param>
/// <param name="From">The from definitions keyed by event type name.</param>
/// <param name="Join">The join definitions keyed by event type name.</param>
/// <param name="Children">The nested children definitions keyed by property path.</param>
/// <param name="All">The from-every definition applied to all events.</param>
/// <param name="FromEventProperty">The optional from-event-property definition.</param>
/// <param name="RemovedWith">The removed-with definitions keyed by event type name.</param>
/// <param name="RemovedWithJoin">The removed-with-join definitions keyed by event type name.</param>
/// <param name="AutoMap">The auto-map setting.</param>
public record ChildrenDefinition(
    string IdentifiedBy,
    IReadOnlyDictionary<string, FromDefinition> From,
    IReadOnlyDictionary<string, JoinDefinition> Join,
    IReadOnlyDictionary<string, ChildrenDefinition> Children,
    FromEveryDefinition All,
    FromEventPropertyDefinition? FromEventProperty,
    IReadOnlyDictionary<string, RemovedWithDefinition> RemovedWith,
    IReadOnlyDictionary<string, RemovedWithJoinDefinition> RemovedWithJoin,
    ProjectionAutoMap AutoMap = ProjectionAutoMap.Inherit);
