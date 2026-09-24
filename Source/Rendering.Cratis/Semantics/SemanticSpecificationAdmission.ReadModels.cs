// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticSpecificationAdmission
{
    static bool HasExpectedProjectionEvent(
        SemanticApplicationContext context,
        SemanticSpecification specification,
        SemanticSpecificationReadModel expected)
    {
        var projection = context.Projections.Values.SingleOrDefault(_ => _.ReadModel == expected.ReadModel);
        return projection?.Transitions.Length == 1 &&
            specification.ThenEvents.Any(_ => _.EventContract == projection.Transitions[0].EventContract);
    }

    static bool ReadModelMatches(SemanticApplicationContext context, SemanticSpecificationReadModel expected) =>
        context.ReadModels.TryGetValue(expected.ReadModel, out var readModel) &&
        ValuesMatch(expected.Values, readModel.Properties) && IsScalar(expected.Key);

    static bool QueryMatches(SemanticApplicationContext context, SemanticSpecificationQueryResult expected) =>
        context.Queries.TryGetValue(expected.Query, out var query) && IsScalar(expected.Key) && expected.Results.Length == 1 &&
        expected.Results.All(result => result.ReadModel == query.ReadModel && ReadModelMatches(context, result));
}
