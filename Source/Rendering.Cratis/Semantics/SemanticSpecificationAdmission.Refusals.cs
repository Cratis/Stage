// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticSpecificationAdmission
{
    static bool ProtectedQuerySourcesMatch(SemanticApplicationContext context, SemanticSpecification specification, SemanticCommand command)
    {
        foreach (var expected in specification.ThenQueries.Where(result =>
            context.Queries.TryGetValue(result.Query, out var query) && IsProtected(context, query)))
        {
            // Keyed lookups still read only their selected stream; collection scenarios preserve all seeded rows.
            if (context.Queries[expected.Query].Cardinality == SemanticQueryCardinality.Many) continue;
            if (specification.GivenEvents.Any(given => !Equals(given.EventSource?.Value, expected.Key)) ||
                SemanticProductionSelection.Taken(context.Application, command, specification).Any(produced => !Equals(SemanticDestinations.ForSpecification(specification, command, produced).Value, expected.Key)))
            {
                return false;
            }
        }

        return true;
    }

    static bool AssertsUncontrolledOccurrence(SemanticApplication application, SemanticSpecification specification, SemanticCommand command) =>
        !SemanticSpecificationClock.TryGet(specification, out _) && SemanticProductionSelection.Taken(application, command, specification).Any(produced => produced.Mappings.Any(mapping =>
            mapping.Source is SemanticEventContextExpression { Value: SemanticEventContextValueKind.Occurred })) &&
        (!specification.ThenEvents.IsEmpty || !specification.ThenReadModels.IsEmpty || !specification.ThenAbsentReadModels.IsEmpty || !specification.ThenQueries.IsEmpty);

    static bool MissingObservableSource(SemanticApplicationContext context, SemanticSpecificationEvent fact) =>
        fact.EventSource is null && SemanticSpecificationEventSources.RequiresSource(context, fact.EventContract, fact.Route is not null);

    // A sourceless given fact has its own code so it is not lost in the generic STAGE-ESM-011 bucket.
    static ArtifactRenderDiagnostic RefusalDiagnostic(SemanticApplicationContext context, SemanticSpecification specification, string reason)
    {
        if (reason != SourcelessGivenReason)
        {
            return new("STAGE-ESM-011", ArtifactRenderDiagnosticSeverity.Error, $"Specification '{specification.Name}' cannot render: {reason}", specification.Id);
        }

        // Projected facts lower a sourceless 'when append' into the givens, so the event may be the appended one.
        var (role, contract) = specification.GivenEvents.FirstOrDefault(fact => MissingObservableSource(context, fact)) is { } given
            ? ("given", given.EventContract)
            : ("appended", specification.WhenAppended?.EventContract);
        var eventName = contract is { } id && context.Events.TryGetValue(id, out var @event) ? $"'{@event.Name}'" : "an event";
        return new(
            SourcelessGivenCode,
            ArtifactRenderDiagnosticSeverity.Error,
            $"Specification '{specification.Name}' cannot render: {role} event {eventName} has no event source. {reason}",
            specification.Id);
    }
}
