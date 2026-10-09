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

    internal static bool CanProjectQueryOnly(SemanticSpecification specification, SemanticApplicationContext context)
    {
        if (specification.When is not null || specification.WhenAppended is not null || specification.GivenCaller is not null ||
            specification.GivenEvents.IsEmpty || !specification.GivenReadModels.IsEmpty || specification.ThenQueries.Length != 1 ||
            !specification.ThenEvents.IsEmpty || !specification.ThenReadModels.IsEmpty || !specification.ThenErrors.IsEmpty ||
            specification.ThenDenied || !specification.ThenAbsentReadModels.IsEmpty)
        {
            return false;
        }

        var expected = specification.ThenQueries[0];
        return context.Queries.TryGetValue(expected.Query, out var query) && query.Authorization is null &&
            !context.Reducers.Any(reducer => reducer.ReadModel == query.ReadModel) && QueryMatches(context, expected) &&
            HasExpectedProjectionEvents(context, specification, expected);
    }

    static bool HasExpectedProjectionEvent(
        SemanticApplicationContext context,
        SemanticSpecification specification,
        SemanticSpecificationReadModel expected)
    {
        var reducer = context.Reducers.SingleOrDefault(_ => _.ReadModel == expected.ReadModel);
        if (reducer is not null)
        {
            var command = context.Commands[specification.When!.Command];

            // Without occurrence identities, unordered duplicates cannot be replayed against
            // the reducer's event-source state without guessing which payload came first.
            if (specification.ThenEventsInAnyOrder && specification.ThenEvents.GroupBy(_ => _.EventContract).Any(group => group.Count() > 1))
                return false;
            return specification.ThenEvents.Length == command.Produces.Length &&
                (specification.GivenEvents.Any(given => reducer.Transitions.Any(_ => _.EventContract == given.EventContract) &&
                    Equals(given.EventSource?.Value, expected.Key)) ||
                command.Produces.Any(produced => reducer.Transitions.Any(_ => _.EventContract == produced.EventContract) &&
                    Equals(SemanticDestinations.ForSpecification(specification, command, produced).Value, expected.Key)));
        }

        var projection = context.Projections.Values.SingleOrDefault(_ => _.ReadModel == expected.ReadModel);
        if (projection?.Scope is { } scope)
        {
            // A scoped projection observes every produced fact, not just the root-from fact. Unordered
            // duplicate contracts cannot be paired with their production occurrences unambiguously.
            var command = context.Commands[specification.When!.Command];
            return specification.ThenEvents.Length == command.Produces.Length &&
                scope.From.Any(from => specification.ThenEvents.Any(@event => @event.EventContract == from.EventContract)) &&
                (!specification.ThenEventsInAnyOrder ||
                    specification.ThenEvents.Select(@event => @event.EventContract).Distinct().Count() == specification.ThenEvents.Length) &&
                command.Produces.All(produced => SemanticDestinations.Of(command, produced) is SemanticResolvedExpression destination &&
                    Equals(
                        SemanticDestinations.ForSpecification(specification, command, produced).Value,
                        specification.When.Values.Single(_ => _.TargetProperty == destination.Target).Value));
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
        context.ReadModels.TryGetValue(query.ReadModel, out var readModel) && (query.Argument is null || IsScalar(expected.Key)) &&
        !CannotCompareScopedPresence(context, query.ReadModel, expected.Exactly, []) &&
        expected.Results.Length > 0 && expected.Results.All(result => result.ReadModel == query.ReadModel &&
            !CannotCompareScopedPresence(context, query.ReadModel, result.Exactly, result.Values) &&
            (query.Cardinality == SemanticQueryCardinality.Many || Equals(result.Key, expected.Key)) &&
            ValuesMatch(result.Values, readModel.Properties, result.Exactly || expected.Exactly) &&
            CanCompareQueryResult(result.Values, readModel.Properties) &&
            IsScalar(result.Key));

    static bool HasExpectedProjectionEvents(SemanticApplicationContext context, SemanticSpecification specification, SemanticSpecificationQueryResult expected)
    {
        if (!context.Queries.TryGetValue(expected.Query, out var query)) return false;
        var projection = context.Projections.Values.SingleOrDefault(_ => _.ReadModel == query.ReadModel);
        if (projection?.Scope is { } scope)
        {
            return expected.Results.All(result => specification.GivenEvents.Any(given =>
                Equals(given.EventSource?.Value, result.Key) && scope.From.Any(from => from.EventContract == given.EventContract)) ||
                specification.GivenEvents.Any(given => scope.From.Any(from => from.EventContract == given.EventContract)));
        }

        return projection?.Transitions.Length == 1 &&
            expected.Results.All(result => specification.GivenEvents.Any(given =>
                given.EventContract == projection.Transitions[0].EventContract && Equals(given.EventSource?.Value, result.Key)));
    }
}
