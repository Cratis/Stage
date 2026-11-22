// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static class SemanticSourcelessGivens
{
    internal const string Code = "STAGE-ESM-051";
    internal const string Reason = "A sourceless given fact has a null destination in the reference world; Chronicle scenario events require an event source id. Tracked by Cratis/Stage#960.";

    internal static bool MissingObservableSource(SemanticApplication application, SemanticSpecificationEvent fact) =>
        fact.EventSource is null && RequiresSource(application, fact.EventContract, fact.Route is not null);

    internal static bool RequiresSource(SemanticApplication application, SemanticId contract, bool routed = false) =>
        routed || SemanticVersionFeatures.Slices(application).Any(slice => SliceRequiresSource(slice, contract));

    internal static bool ProjectionRequiresSource(SemanticProjection projection, SemanticId contract) => projection.Scope is { } scope
        ? ObservesSource(scope, contract, false)
        : projection.Transitions.Any(transition => transition.EventContract == contract);

    static bool SliceRequiresSource(SemanticSlice slice, SemanticId contract) =>
        slice.Constraints.Any(constraint => ConstraintRequiresSource(constraint, contract)) ||
        slice.Reducers.Any(reducer => reducer.Transitions.Any(transition => transition.EventContract == contract)) ||
        slice.Reactions.Any(reaction => reaction.Triggers.Any(trigger => trigger.Kind == SemanticReactionTriggerKind.Event && trigger.Source == contract)) ||
        slice.Commands.Any(command => CommandObservesRoute(command, contract)) ||
        slice.Projections.Any(projection => ProjectionRequiresSource(projection, contract));

    static bool ConstraintRequiresSource(SemanticConstraint constraint, SemanticId contract) =>
        constraint.Targets.Any(target => target.EventContract == contract) || constraint.ReleasedBy.Contains(contract);

    static bool CommandObservesRoute(SemanticCommand command, SemanticId contract) =>
        command.Produces.Any(produced => produced.EventContract == contract &&
            (command.Route is not null || produced.Route is not null));

    static bool ObservesSource(SemanticProjectionScope scope, SemanticId contract, bool child) =>
        EveryObservesSource(scope, contract) ||
        scope.From.Any(contribution => ObservesSource(contribution, contract, child)) ||
        scope.Removals.Any(removal => ObservesSource(removal, contract, child)) ||
        scope.Joins.Any(join => join.EventContract == contract) ||
        scope.JoinRemovals.Any(removal => removal.EventContract == contract && ObservesSource(removal.Key)) ||
        scope.Children.Any(nested => ObservesSource(nested.Scope, contract, true)) ||
        scope.Nested.Any(nested => ObservesSource(nested.Scope, contract, child));

    static bool EveryObservesSource(SemanticProjectionScope scope, SemanticId contract) =>
        scope.Every is { SubscribesToAllEvents: true } ||
        (scope.Every is { } every && Projections.ProjectionReferencedEventNamesAreUnique.ScopeEvents(scope).Contains(contract) &&
            every.Mappings.Any(ReadsSource));

    static bool ObservesSource(SemanticProjectionFrom contribution, SemanticId contract, bool child) =>
        contribution.EventContract == contract &&
        (ObservesSource(contribution.Key) || ObservesParent(contribution.ParentKey, child) || contribution.Mappings.Any(ReadsSource));

    static bool ObservesSource(SemanticProjectionRemoval removal, SemanticId contract, bool child) =>
        removal.EventContract == contract && (ObservesSource(removal.Key) || ObservesParent(removal.ParentKey, child));

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
