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

    internal static bool CannotCompareScopedPresence(
        SemanticApplicationContext context,
        SemanticId readModel,
        bool exactly,
        IEnumerable<SemanticPropertyValue> values) =>
        context.Projections.Values.Any(projection => projection.ReadModel == readModel && projection.Scope is not null) &&
        (exactly || values.Any(value => value.Value is SemanticNullValue));

    static bool HasExpectedProjectionEvent(
        SemanticApplicationContext context,
        SemanticSpecification specification,
        SemanticSpecificationReadModel expected)
    {
        var projection = context.Projections.Values.SingleOrDefault(_ => _.ReadModel == expected.ReadModel);
        if (projection?.Scope is { } scope)
        {
            // A produced root-from event creates the instance being compared. Other scoped events are
            // replayed from the given world, never synthesized from a flat transition.
            return specification.ThenEvents.Count(expectedEvent =>
                scope.From.Any(from => from.EventContract == expectedEvent.EventContract)) == 1;
        }

        return projection?.Transitions.Length == 1 &&
            specification.ThenEvents.Count(_ => _.EventContract == projection.Transitions[0].EventContract) == 1;
    }

    static bool ReadModelMatches(SemanticApplicationContext context, SemanticSpecificationReadModel expected) =>
        !CannotCompareScopedPresence(context, expected.ReadModel, expected.Exactly, expected.Values) &&
        context.ReadModels.TryGetValue(expected.ReadModel, out var readModel) &&
        ValuesMatch(expected.Values, readModel.Properties, expected.Exactly) && IsScalar(expected.Key) &&
        readModel.Properties.SingleOrDefault(_ => _.IsIdentifier) is { } identifier &&
        (!expected.Values.Any(_ => _.TargetProperty == identifier.Id) ||
            Equals(expected.Key, expected.Values.Single(_ => _.TargetProperty == identifier.Id).Value));

    static bool QueryMatches(SemanticApplicationContext context, SemanticSpecificationQueryResult expected) =>
        context.Queries.TryGetValue(expected.Query, out var query) &&
        context.ReadModels.TryGetValue(query.ReadModel, out var readModel) && IsScalar(expected.Key) &&
        !CannotCompareScopedPresence(context, query.ReadModel, expected.Exactly, []) &&
        expected.Results.Length == 1 && expected.Results.All(result => result.ReadModel == query.ReadModel &&
            !CannotCompareScopedPresence(context, query.ReadModel, result.Exactly, result.Values) &&
            Equals(result.Key, expected.Key) &&
            ValuesMatch(result.Values, readModel.Properties, result.Exactly || expected.Exactly) &&
            CanCompareQueryResult(result.Values, readModel.Properties) &&
            IsScalar(result.Key));
}
