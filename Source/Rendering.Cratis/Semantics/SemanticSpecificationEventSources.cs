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
        SemanticSpecificationAdmission.EventReactionTriggers(context).Any(trigger => trigger.Source == contract) ||
        context.Commands.Values.Any(command => command.Produces.Any(produced => produced.EventContract == contract &&
            (command.Route is not null || produced.Route is not null))) ||
        context.Projections.Values.Any(projection => ObservesSource(projection, contract));

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

    static bool ObservesSource(SemanticProjection projection, SemanticId contract) => projection.Scope is { } scope
        ? ObservesSource(scope, contract, false)
        : projection.Transitions.Any(transition => transition.EventContract == contract);

    static bool ObservesSource(SemanticProjectionScope scope, SemanticId contract, bool child)
    {
        if (scope.Every is { SubscribesToAllEvents: true })
        {
            return true;
        }
        if (scope.Every is { } every && Projections.ProjectionReferencedEventNamesAreUnique.ScopeEvents(scope).Contains(contract) &&
            every.Mappings.Any(ReadsSource))
        {
            return true;
        }

        return scope.From.Any(from => from.EventContract == contract &&
                (ObservesSource(from.Key) || ObservesParent(from.ParentKey, child) || from.Mappings.Any(ReadsSource))) ||
            scope.Removals.Any(removal => removal.EventContract == contract &&
                (ObservesSource(removal.Key) || ObservesParent(removal.ParentKey, child))) ||
            scope.Joins.Any(join => join.EventContract == contract) ||
            scope.JoinRemovals.Any(removal => removal.EventContract == contract && ObservesSource(removal.Key)) ||
            scope.Children.Any(nested => ObservesSource(nested.Scope, contract, true)) ||
            scope.Nested.Any(nested => ObservesSource(nested.Scope, contract, child));
    }

    static bool ObservesParent(SemanticProjectionKey? key, bool child) => key is null ? child : ObservesSource(key);

    static bool ObservesSource(SemanticProjectionKey key) => key switch
    {
        SemanticProjectionValueKey value => !Independent(value.Value),
        SemanticProjectionPartsKey parts => parts.Parts.Any(part => !Independent(part.Value)),
        SemanticProjectionCompositeKey composite => composite.Parts.Any(part => !Independent(part.Value)),
        _ => true
    };

    static bool ReadsSource(SemanticProjectionMapping mapping) =>
        (mapping.Source is { } source && !Independent(source)) || (mapping.Factor is { } factor && !Independent(factor));

    static bool Independent(SemanticProjectionValue value) => value is SemanticProjectionEventProperty or SemanticProjectionLiteral;
}
