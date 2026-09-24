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
            var valid = HasRenderableCallerAndCommand(context, specification, out var command) &&
                HasRenderableGivenEvents(context, specification) && GivenKeysMatchProjectedProperties(context, specification) && specification.GivenReadModels.IsEmpty &&
                ValuesMatch(specification.When!.Values, command?.Properties ?? []) &&
                HasOneOutcome(specification) && HasSupportedCounts(specification) &&
                (!specification.ThenEventsInAnyOrder || specification.ThenEvents.Length <= 1) &&
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
                specification.ThenErrors.All(_ => _.Code is null && SemanticValidationRendering.SafeMessage(_.Message)) &&
                HasRenderableEventSources(context, specification, command!);

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

    static string RejectionReason(SemanticApplicationContext context, SemanticSpecification specification)
    {
        if (!specification.GivenReadModels.IsEmpty)
        {
            return "Given read-model state replaces projected state in the reference world, but a Chronicle ReadModelScenario only seeds lookup interception, not the projection's initial state.";
        }

        if (specification.GivenEvents.Any(_ => _.EventSource is null))
        {
            return "A sourceless given fact has a null destination in the reference world; Chronicle scenario events require an event source id.";
        }

        if (specification.ThenReadModels.Any(_ => context.Projections.Values.Any(projection => projection.ReadModel == _.ReadModel && projection.Scope is not null)) ||
            specification.ThenQueries.Any(_ => context.Queries.TryGetValue(_.Query, out var query) && context.Projections.Values.Any(projection => projection.ReadModel == query.ReadModel && projection.Scope is not null)))
        {
            return "Scoped projection specifications need event replay that reproduces the reference projection scope; flat transition replay is not equivalent.";
        }

        return specification.ThenEventsInAnyOrder && specification.ThenEvents.Length > 1
            ? "Unordered expectations need an exact multiset comparison, including duplicate events and event sources."
            : "The scenario cannot preserve all of its declared fixtures or expectations.";
    }
}
