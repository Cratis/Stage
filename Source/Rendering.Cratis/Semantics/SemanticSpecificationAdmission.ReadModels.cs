// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Checks read-model and query expectations.
/// </summary>
internal static partial class SemanticSpecificationAdmission
{
    internal static bool CanCompareQueryResult(
        IEnumerable<SemanticPropertyValue> values,
        IReadOnlyList<SemanticProperty> properties) =>
        values.All(value => !properties.Single(property => property.Id == value.TargetProperty).Type.IsCollection);

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
        ValuesMatch(expected.Values, readModel.Properties, expected.Exactly) && IsScalar(expected.Key) &&
        readModel.Properties.SingleOrDefault(_ => _.IsIdentifier) is { } identifier &&
        (!expected.Values.Any(_ => _.TargetProperty == identifier.Id) ||
            Equals(expected.Key, expected.Values.Single(_ => _.TargetProperty == identifier.Id).Value));

    static bool QueryMatches(SemanticApplicationContext context, SemanticSpecificationQueryResult expected) =>
        context.Queries.TryGetValue(expected.Query, out var query) &&
        context.ReadModels.TryGetValue(query.ReadModel, out var readModel) && IsScalar(expected.Key) &&
        expected.Results.Length == 1 && expected.Results.All(result => result.ReadModel == query.ReadModel &&
            Equals(result.Key, expected.Key) &&
            ValuesMatch(result.Values, readModel.Properties, result.Exactly || expected.Exactly) &&
            CanCompareQueryResult(result.Values, readModel.Properties) &&
            IsScalar(result.Key));
}
