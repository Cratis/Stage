// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static class SemanticSpecificationEventSources
{
    internal const string Placeholder = "when-append";

    internal static bool RequiresSource(SemanticApplicationContext context, SemanticId contract, bool routed = false) =>
        routed || context.Reducers.Any(reducer => reducer.Transitions.Any(transition => transition.EventContract == contract)) ||
        SemanticSpecificationAdmission.IsReactionConstraintTarget(context, contract) ||
        ReactionObservesSource(context, contract) || RoutedProductionObservesSource(context, contract) ||
        context.Projections.Values.Any(projection => SemanticSourcelessGivens.ProjectionRequiresSource(projection, contract));

    internal static string Expression(SemanticApplicationContext context, SemanticTypeSystem types, SemanticSpecificationEvent fact)
    {
        if (fact.EventSource is { } source)
        {
            return types.EventSourceExpression(types.Value(source.Value, source.Type), source.Type);
        }
        if (RequiresSource(context, fact.EventContract, fact.Route is not null))
        {
            throw new InvalidOperationException("An observable event source must be supplied before rendering a specification.");
        }

        // A transport fixture only, never an authored identity or a guessed payload property.
        return CSharpCodeBuilder.StringLiteral(Placeholder);
    }

    static bool ReactionObservesSource(SemanticApplicationContext context, SemanticId contract) =>
        SemanticVersionFeatures.Slices(context.Request.Model.Application)
            .SelectMany(slice => slice.Reactions)
            .SelectMany(reaction => reaction.Triggers)
            .Any(trigger => trigger.Kind == SemanticReactionTriggerKind.Event && trigger.Source == contract);

    static bool RoutedProductionObservesSource(SemanticApplicationContext context, SemanticId contract) =>
        context.Commands.Values.Any(command => command.Produces.Any(produced => produced.EventContract == contract &&
            (command.Route is not null || produced.Route is not null)));
}
