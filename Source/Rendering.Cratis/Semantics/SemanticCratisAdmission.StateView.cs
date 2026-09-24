// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticCratisAdmission
{
    static void ValidateStateView(
        SemanticApplicationContext context,
        SemanticSlice slice,
        List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (slice.ReadModels.Length != 1 || slice.Projections.Length != 1 || slice.Queries.Length > 1)
        {
            diagnostics.Add(Error("STAGE-ESM-007", $"State-view slice '{slice.Name}' exceeds the first Cratis read capability.", slice.Id));
            return;
        }

        var readModel = slice.ReadModels[0];
        var projection = slice.Projections[0];
        if (projection.ReadModel != readModel.Id || projection.Transitions.Length != 1 || readModel.Properties.Any(_ => !TypeExists(context, _.Type)))
        {
            diagnostics.Add(Error("STAGE-ESM-008", $"Projection '{projection.Name}' does not have one resolvable read-model transition.", projection.Id));
            return;
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

        if (slice.Queries.SingleOrDefault() is { } query && ValidateQueryAuthorization(query, diagnostics))
        {
            var identifiers = readModel.Properties.Where(_ => _.IsIdentifier).ToArray();
            if (query.ReadModel != readModel.Id || query.Cardinality != SemanticQueryCardinality.ZeroOrOne ||
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
