// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Checks keyed absence assertions against the projection scenario's materialized instances.
/// </summary>
internal static partial class SemanticSpecificationAdmission
{
    const string AbsenceRejection = "Read-model absence requires a projection with a scalar text or UUID identifier and faithful event replay; seeded read-model state, reducers and ambiguous unordered duplicate productions are not supported.";

    static bool CanAssertAbsence(
        SemanticApplicationContext context,
        SemanticSpecification specification,
        SemanticSpecificationAbsentReadModel expected) =>
        context.ReadModels.TryGetValue(expected.ReadModel, out var readModel) &&
        readModel.Properties.Count(property => property.IsIdentifier) == 1 &&
        IsScalar(expected.Key) && IsLosslessEventSource(context, readModel.Properties.Single(property => property.IsIdentifier).Type) &&
        context.Projections.Values.Any(projection => projection.ReadModel == expected.ReadModel) &&
        !context.Reducers.Any(reducer => reducer.ReadModel == expected.ReadModel) &&
        specification.GivenReadModels.IsEmpty &&
        (!specification.ThenEventsInAnyOrder || specification.ThenEvents.Select(expected => expected.EventContract).Distinct().Count() == specification.ThenEvents.Length);
}
