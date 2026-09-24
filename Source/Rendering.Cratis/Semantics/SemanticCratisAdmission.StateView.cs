// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Admits the supported state-view shape.
/// </summary>
internal static partial class SemanticCratisAdmission
{
    static void ValidateStateView(
        SemanticApplicationContext context,
        SemanticSlice slice,
        List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (slice.ReadModels.Length == 0 || slice.Projections.Length != slice.ReadModels.Length ||
            slice.Projections.Select(_ => _.ReadModel).Distinct().Count() != slice.ReadModels.Length ||
            slice.Projections.Any(_ => !slice.ReadModels.Any(model => model.Id == _.ReadModel)))
        {
            diagnostics.Add(Error("STAGE-ESM-007", $"State-view slice '{slice.Name}' needs exactly one projection for each read model.", slice.Id));
            return;
        }

        foreach (var readModel in slice.ReadModels)
        {
            var projection = slice.Projections.Single(_ => _.ReadModel == readModel.Id);
            if (readModel.Properties.Any(_ => !TypeExists(context, _.Type)) || readModel.Properties.Count(_ => _.IsIdentifier) != 1)
            {
                diagnostics.Add(Error("STAGE-ESM-008", $"Projection '{projection.Name}' needs a resolvable read model with one identifier.", projection.Id));
                continue;
            }

            if (projection.Scope is { } scope)
            {
                var rejection = SemanticScopedProjectionSupport.Rejection(scope, context, readModel.Properties);
                if (rejection is not null || !projection.Transitions.IsEmpty)
                {
                    diagnostics.Add(Error("STAGE-ESM-017", $"Projection '{projection.Name}' cannot render: {rejection ?? "A scope cannot also have flat transitions."}", projection.Id));
                }

                continue;
            }

            if (projection.Transitions.Length != 1)
            {
                diagnostics.Add(Error("STAGE-ESM-008", $"Projection '{projection.Name}' does not have one resolvable read-model transition.", projection.Id));
                continue;
            }

            var transition = projection.Transitions[0];
            if (!context.Events.TryGetValue(transition.EventContract, out var @event) ||
                transition.AffectedInstance.Cardinality != AffectedInstanceCardinality.One ||
                !IsProperty(transition.AffectedInstance.Key, SemanticExpressionRootKind.Event, @event.Properties.Select(_ => _.Id)) ||
                !MappingsMatch(transition.Mappings, readModel.Properties, @event.Properties, SemanticExpressionRootKind.Event) ||
                !UsesEventSourceIdentity(context, transition, @event, readModel))
            {
                diagnostics.Add(Error("STAGE-ESM-009", $"Projection '{projection.Name}' cannot preserve its affected instance with model-bound Cratis projection semantics.", projection.Id));
            }
        }

        foreach (var query in slice.Queries.Where(query => ValidateQueryAuthorization(context, query, diagnostics)))
        {
            var readModel = slice.ReadModels.SingleOrDefault(_ => _.Id == query.ReadModel);
            var identifiers = readModel?.Properties.Where(_ => _.IsIdentifier).ToArray() ?? [];
            if (query.Cardinality != SemanticQueryCardinality.ZeroOrOne ||
                query.Delivery != SemanticQueryDelivery.Snapshot || identifiers.Length != 1 ||
                query.KeyProperty != identifiers[0].Id || !TypeExists(context, query.Argument.Type))
            {
                diagnostics.Add(Error("STAGE-ESM-010", $"Query '{query.Name}' is not an optional snapshot lookup by the read-model identifier.", query.Id));
            }
        }
    }

    static bool UsesEventSourceIdentity(
        SemanticApplicationContext context,
        SemanticProjectionTransition transition,
        SemanticEventContract @event,
        SemanticReadModel readModel)
    {
        var eventKey = ((SemanticResolvedExpression)transition.AffectedInstance.Key).Target;
        var eventProperty = @event.Properties.Single(_ => _.Id == eventKey);
        var readModelKeys = readModel.Properties.Where(_ => _.IsIdentifier).ToArray();
        if (readModelKeys.Length != 1 || eventProperty.Type.Kind != SemanticTypeReferenceKind.Concept ||
            !context.IdentifierConcepts.Contains(eventProperty.Type.Target))
        {
            return false;
        }

        var keyMapping = transition.Mappings.SingleOrDefault(_ => _.TargetProperty == readModelKeys[0].Id);
        if (!IsProperty(keyMapping?.Source, SemanticExpressionRootKind.Event, [eventKey]))
        {
            return false;
        }

        var producers = context.Commands.Values.SelectMany(command =>
            command.Produces.Where(produced => produced.EventContract == @event.Id).Select(produced => (Command: command, Produced: produced))).ToArray();
        return producers.Length > 0 && producers.All(producer =>
            SemanticDestinations.Of(producer.Command, producer.Produced) is SemanticResolvedExpression destination &&
            producer.Produced.Mappings.SingleOrDefault(mapping => mapping.TargetProperty == eventKey)?.Source is SemanticResolvedExpression source &&
            destination.Root == SemanticExpressionRootKind.Command && source.Root == SemanticExpressionRootKind.Command &&
            destination.Target == source.Target);
    }
}
