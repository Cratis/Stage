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
        if (specification.When is not null || specification.WhenAppended is not null ||
            specification.GivenEvents.IsEmpty || !specification.GivenReadModels.IsEmpty || specification.ThenQueries.Length != 1 ||
            !specification.ThenEvents.IsEmpty || !specification.ThenReadModels.IsEmpty || !specification.ThenErrors.IsEmpty ||
            specification.ThenDenied || !specification.ThenAbsentReadModels.IsEmpty)
        {
            return false;
        }

        var expected = specification.ThenQueries[0];
        return ProtectedCollectionRejectionReason(context, specification) is null &&
            context.Queries.TryGetValue(expected.Query, out var query) &&
            (query.Authorization is null ? specification.GivenCaller is null :
                query.Cardinality == SemanticQueryCardinality.Many && specification.GivenCaller is { } caller &&
                !caller.Claims.Any(claim => SemanticCratisAdmission.IsRoleClaim(claim.Type)) &&
                HasRenderableGivenEvents(context, specification) && GivenKeysMatchProjectedProperties(context, specification)) &&
            !context.Reducers.Any(reducer => reducer.ReadModel == query.ReadModel) && QueryMatches(context, expected) &&
            HasExpectedProjectionEvents(context, specification, expected);
    }

    static bool HasExpectedProjectionEvent(
        SemanticApplicationContext context,
        SemanticSpecification specification,
        SemanticSpecificationReadModel expected)
    {
        if (CanSeedProjectedAbsence(context, specification) && SemanticProjectionSpecificationSeeds.Contains(specification, expected)) return true;
        var reducer = context.Reducers.SingleOrDefault(_ => _.ReadModel == expected.ReadModel);
        if (reducer is not null)
        {
            var command = context.Commands[specification.When!.Command];

            // Without occurrence identities, unordered duplicates cannot be replayed against
            // the reducer's event-source state without guessing which payload came first.
            if (specification.ThenEventsInAnyOrder && specification.ThenEvents.GroupBy(_ => _.EventContract).Any(group => group.Count() > 1))
                return false;
            return specification.ThenEvents.Length == SemanticProductionSelection.Taken(context.Application, command, specification).Length &&
                (specification.GivenEvents.Any(given => reducer.Transitions.Any(_ => _.EventContract == given.EventContract) &&
                    Equals(given.EventSource?.Value, expected.Key)) ||
                SemanticProductionSelection.Taken(context.Application, command, specification).Any(produced => reducer.Transitions.Any(_ => _.EventContract == produced.EventContract) &&
                    Equals(SemanticDestinations.ForSpecification(specification, command, produced).Value, expected.Key)));
        }

        var projection = context.Projections.Values.SingleOrDefault(_ => _.ReadModel == expected.ReadModel);
        if (projection?.Scope is { } scope)
        {
            // A scoped projection observes every produced fact, not just the root-from fact. Unordered
            // duplicate contracts cannot be paired with their production occurrences unambiguously.
            var command = context.Commands[specification.When!.Command];
            return specification.ThenEvents.Length == SemanticProductionSelection.Taken(context.Application, command, specification).Length &&
                (scope.From.Any(from => specification.ThenEvents.Any(@event => @event.EventContract == from.EventContract)) ||
                    FedByGivenFacts(specification, scope, expected)) &&
                (!specification.ThenEventsInAnyOrder ||
                    specification.ThenEvents.Select(@event => @event.EventContract).Distinct().Count() == specification.ThenEvents.Length) &&
                SemanticProductionSelection.Taken(context.Application, command, specification).All(produced => SemanticDestinations.Of(command, produced) is SemanticResolvedExpression destination &&
                    Equals(
                        SemanticDestinations.ForSpecification(specification, command, produced).Value,
                        specification.When.Values.Single(_ => _.TargetProperty == destination.Target).Value));
        }

        return projection?.Transitions.Length == 1 &&
            specification.ThenEvents.Count(_ => _.EventContract == projection.Transitions[0].EventContract) == 1;
    }

    // The scenario seeds the given facts next to the produced ones, so a view the given facts of its own key feed is projected faithfully.
    static bool FedByGivenFacts(SemanticSpecification specification, SemanticProjectionScope scope, SemanticSpecificationReadModel expected) =>
        specification.GivenEvents.Any(given => Equals(given.EventSource?.Value, expected.Key) &&
            (scope.From.Any(from => from.EventContract == given.EventContract) || scope.Removals.Any(removal => removal.EventContract == given.EventContract)));

    static bool ReadModelMatches(SemanticApplicationContext context, SemanticSpecificationReadModel expected) =>
        !CannotCompareScopedPresence(context, expected.ReadModel, expected.Exactly, expected.Values) &&
        context.ReadModels.TryGetValue(expected.ReadModel, out var readModel) &&
        ValuesMatch(expected.Values, readModel.Properties, expected.Exactly) && ReadModelKeyMatches(readModel, expected);

    static bool ReadModelKeyMatches(SemanticReadModel readModel, SemanticSpecificationReadModel expected)
    {
        var identifiers = readModel.Properties.Where(property => property.IsIdentifier).ToArray();
        if (identifiers.Length == 1)
        {
            return IsScalar(expected.Key) && (!expected.Values.Any(value => value.TargetProperty == identifiers[0].Id) ||
                Equals(expected.Key, expected.Values.Single(value => value.TargetProperty == identifiers[0].Id).Value));
        }
        return identifiers.Length > 1 && expected.Key is SemanticCompositeValue composite && composite.Properties.Length == identifiers.Length &&
            identifiers.All(identifier => composite.Properties.SingleOrDefault(part => part.TargetProperty == identifier.Id) is { } part &&
                IsScalar(part.Value) && (!expected.Values.Any(value => value.TargetProperty == identifier.Id) ||
                    Equals(part.Value, expected.Values.Single(value => value.TargetProperty == identifier.Id).Value)));
    }

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
