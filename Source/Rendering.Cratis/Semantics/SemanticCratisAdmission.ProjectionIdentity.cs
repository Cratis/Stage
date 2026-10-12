// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Proves flat projection keys match the append identity at every modeled production site.
/// </summary>
internal static partial class SemanticCratisAdmission
{
    static bool UsesEventSourceIdentity(
        SemanticApplicationContext context,
        SemanticProjectionTransition transition,
        SemanticEventContract @event,
        SemanticReadModel readModel)
    {
        var eventKey = ((SemanticResolvedExpression)transition.AffectedInstance.Key).Target;
        var eventProperty = @event.Properties.Single(property => property.Id == eventKey);
        var readModelKeys = readModel.Properties.Where(property => property.IsIdentifier).ToArray();
        if (readModelKeys.Length != 1 || eventProperty.Type.Kind != SemanticTypeReferenceKind.Concept ||
            !context.IdentifierConcepts.Contains(eventProperty.Type.Target))
        {
            return false;
        }

        var keyMapping = transition.Mappings.SingleOrDefault(mapping => mapping.TargetProperty == readModelKeys[0].Id);
        if (!IsProperty(keyMapping?.Source, SemanticExpressionRootKind.Event, [eventKey]))
        {
            return false;
        }

        return EveryProducerUsesProjectionKey(context, @event.Id, eventKey);
    }

    static bool EveryProducerUsesProjectionKey(SemanticApplicationContext context, SemanticId contract, SemanticId eventKey)
    {
        var commands = context.Commands.Values.SelectMany(command =>
            command.Produces.Where(produced => produced.EventContract == contract).Select(produced => (Command: command, Produced: produced))).ToArray();
        var reactions = SemanticReactionProductions.In(context.Application)
            .Where(production => production.Produced.EventContract == contract).ToArray();

        return commands.Length + reactions.Length > 0 &&
            commands.All(producer => CommandUsesProjectionKey(producer.Command, producer.Produced, eventKey)) &&
            reactions.All(producer => ReactionUsesProjectionKey(producer.Produced, eventKey));
    }

    static bool CommandUsesProjectionKey(SemanticCommand command, SemanticProducedEvent produced, SemanticId eventKey) =>
        SemanticDestinations.Of(command, produced) is SemanticResolvedExpression destination &&
        produced.Mappings.SingleOrDefault(mapping => mapping.TargetProperty == eventKey)?.Source is SemanticResolvedExpression source &&
        destination.Root == SemanticExpressionRootKind.Command && source.Root == SemanticExpressionRootKind.Command &&
        destination.Target == source.Target;

    static bool ReactionUsesProjectionKey(SemanticProducedEvent produced, SemanticId eventKey)
    {
        var source = produced.Mappings.SingleOrDefault(mapping => mapping.TargetProperty == eventKey)?.Source;

        // An omitted destination uses the triggering append's source, not a payload property.
        // Only an explicit destination identical to the payload-key mapping proves this identity.
        return produced.Destination is not null && produced.Destination == source;
    }
}
