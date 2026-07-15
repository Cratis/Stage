// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Projections;

/// <summary>
/// Represents a projection that maps events to a read model, compatible with Chronicle's projection engine.
/// </summary>
/// <remarks>
/// Dictionaries are keyed by event type name. The engine assigns the projection identifier, owner and event sequence
/// when it registers the projection with Chronicle; those are not part of the authored shape.
/// </remarks>
/// <param name="IsActive">Whether the projection actively observes (a passive projection is materialized on demand).</param>
/// <param name="IsRewindable">Whether the projection may be rewound and rebuilt.</param>
/// <param name="InitialModelState">The initial state for new model instances as a JSON object string.</param>
/// <param name="From">The from definitions keyed by event type name.</param>
/// <param name="Join">The join definitions keyed by event type name.</param>
/// <param name="Children">The children definitions keyed by property path.</param>
/// <param name="FromDerivatives">The from-derivatives definitions.</param>
/// <param name="FromEvery">The from-every definition applied to all events.</param>
/// <param name="FromEventProperty">The optional from-event-property definition.</param>
/// <param name="RemovedWith">The removed-with definitions keyed by event type name.</param>
/// <param name="RemovedWithJoin">The removed-with-join definitions keyed by event type name.</param>
/// <param name="Tags">The tags associated with the projection.</param>
/// <param name="AutoMap">The auto-map setting.</param>
public record ProjectionDefinition(
    bool IsActive,
    bool IsRewindable,
    string InitialModelState,
    IReadOnlyDictionary<string, FromDefinition> From,
    IReadOnlyDictionary<string, JoinDefinition> Join,
    IReadOnlyDictionary<string, ChildrenDefinition> Children,
    IReadOnlyList<FromDerivativesDefinition> FromDerivatives,
    FromEveryDefinition FromEvery,
    FromEventPropertyDefinition? FromEventProperty,
    IReadOnlyDictionary<string, RemovedWithDefinition> RemovedWith,
    IReadOnlyDictionary<string, RemovedWithJoinDefinition> RemovedWithJoin,
    IReadOnlyList<string> Tags,
    ProjectionAutoMap AutoMap = ProjectionAutoMap.Enabled);
