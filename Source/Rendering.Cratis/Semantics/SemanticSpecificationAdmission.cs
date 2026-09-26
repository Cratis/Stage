// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Admits semantic specifications the generated Cratis scenario family can preserve.
/// </summary>
internal static partial class SemanticSpecificationAdmission
{
    /// <summary>
    /// Validates the specifications declared by one slice.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="slice">The declaring slice.</param>
    /// <param name="diagnostics">The diagnostics to append to.</param>
    public static void Validate(
        SemanticApplicationContext context,
        SemanticSlice slice,
        ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var specification in slice.Specifications)
        {
            var valid = CanDenyQueryOnly(specification, context) ||
                (CanSeedQueryOnly(specification, context) && QueryMatches(context, specification.ThenQueries[0])) ||
                (HasRenderableCallerAndCommand(context, specification, out var command) &&
                    HasRenderableGivenEvents(context, specification) && GivenKeysMatchProjectedProperties(context, specification) &&
                    !AssertsUncontrolledOccurrence(specification, command!) &&
                    !GivenEventsViolateConstraints(context, specification) && specification.GivenReadModels.IsEmpty &&
                    ValuesMatch(specification.When!.Values, command?.Properties ?? []) &&
                    HasOneOutcome(specification) && HasSupportedCounts(specification) &&
                    (specification.ThenDenied || !specification.ThenErrors.IsEmpty || specification.ThenEvents.Length == command!.Produces.Length) &&
                    specification.ThenEvents.All(expected => EventMatches(context, expected)) &&
                    (specification.ThenDenied || !specification.ThenErrors.IsEmpty ||
                        (specification.ThenEventsInAnyOrder
                            ? specification.ThenEvents.GroupBy(_ => _.EventContract).All(group =>
                                command!.Produces.Count(_ => _.EventContract == group.Key) == group.Count())
                            : specification.ThenEvents.Select(_ => _.EventContract).SequenceEqual(command!.Produces.Select(_ => _.EventContract)))) &&
                    specification.ThenReadModels.All(expected => ReadModelMatches(context, expected) &&
                        HasExpectedProjectionEvent(context, specification, expected)) &&
                    specification.ThenQueries.All(expected => QueryMatches(context, expected) &&
                        HasExpectedProjectionEvent(context, specification, expected.Results.Single())) &&
                    HasRenderableErrors(context, specification, command) &&
                    HasRenderableEventSources(context, specification, command!) &&
                    ProtectedQuerySourcesMatch(context, specification, command!));

            if (!valid)
            {
                var reason = RejectionReason(context, specification);
                diagnostics.Add(new(
                    "STAGE-ESM-011",
                    ArtifactRenderDiagnosticSeverity.Error,
                    $"Specification '{specification.Name}' cannot render: {reason}",
                    specification.Id));
            }
        }
    }

    static bool ProtectedQuerySourcesMatch(SemanticApplicationContext context, SemanticSpecification specification, SemanticCommand command)
    {
        foreach (var expected in specification.ThenQueries.Where(result =>
            context.Queries.TryGetValue(result.Query, out var query) && query.Authorization is not null))
        {
            // Arc's QueryScenario seeds only the stream identified by the query key, unlike the
            // unprotected ReadModelScenario. Never admit a fixture whose other streams disappear.
            if (specification.GivenEvents.Any(given => !Equals(given.EventSource?.Value, expected.Key)) ||
                command.Produces.Any(produced => !Equals(SemanticDestinations.ForSpecification(specification, command, produced).Value, expected.Key)))
            {
                return false;
            }
        }

        return true;
    }

    static bool AssertsUncontrolledOccurrence(SemanticSpecification specification, SemanticCommand command) =>
        command.Produces.Any(produced => produced.Mappings.Any(mapping =>
            mapping.Source is SemanticEventContextExpression { Value: SemanticEventContextValueKind.Occurred })) &&
        (!specification.ThenEvents.IsEmpty || !specification.ThenReadModels.IsEmpty || !specification.ThenQueries.IsEmpty);

    static string RejectionReason(SemanticApplicationContext context, SemanticSpecification specification)
    {
        if (specification.When is { } when && context.Commands.TryGetValue(when.Command, out var command) &&
            AssertsUncontrolledOccurrence(specification, command))
        {
            return "A command maps $context.occurred from its current clock; fixed event or projected values in a specification cannot assert this occurrence without a supplied time.";
        }

        if (specification.ThenReadModels.Any(_ => CannotCompareScopedPresence(context, _.ReadModel, _.Exactly, _.Values)) ||
            specification.ThenQueries.Any(_ => context.Queries.TryGetValue(_.Query, out var query) &&
                (CannotCompareScopedPresence(context, query.ReadModel, _.Exactly, []) ||
                    _.Results.Any(result => CannotCompareScopedPresence(context, query.ReadModel, result.Exactly, result.Values)))))
        {
            return "An exactly or null comparison of a scoped projection must distinguish unset properties from CLR defaults; the generated ReadModelScenario exposes only the materialized record.";
        }

        if (specification.ThenDenied && specification.When is null)
        {
            return "A denied query must be executed through Arc's query pipeline to assert Unauthorized and no returned data; a direct read-model lookup does not evaluate its policy.";
        }

        if (specification.ThenQueries.Any(_ => context.Queries.TryGetValue(_.Query, out var query) && query.Authorization is not null))
        {
            return "A protected query must run through Arc's query pipeline with the fixture caller; direct invocation bypasses the generated authorization policy.";
        }

        if (specification.When is { } action && context.Commands.TryGetValue(action.Command, out var producingCommand) &&
            !ProtectedQuerySourcesMatch(context, specification, producingCommand))
        {
            return "A protected query scenario replays only events from its query key; every seeded and produced event must use that source.";
        }

        if (!specification.GivenReadModels.IsEmpty)
        {
            return "Given read-model state replaces projected state before the action in the reference world. Chronicle's scenario seed serves lookup interception but cannot initialize each projected instance for subsequent command events; only a query-only lookup of one complete seeded record is supported.";
        }

        if (specification.GivenEvents.Any(_ => _.EventSource is null))
        {
            return "A sourceless given fact has a null destination in the reference world; Chronicle scenario events require an event source id.";
        }

        if (GivenEventsViolateConstraints(context, specification))
        {
            return "Given events violate a selected append-time constraint and cannot be seeded faithfully into Chronicle's event log.";
        }

        if (specification.GivenCaller?.Claims.Any(claim => SemanticCratisAdmission.IsRoleClaim(claim.Type)) == true)
        {
            return "A caller claim using the role claim URI conflates separate Screenplay roles and claims in Arc.";
        }

        return "The scenario cannot preserve all of its declared fixtures or expectations.";
    }
}
