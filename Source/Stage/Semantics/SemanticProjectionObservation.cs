// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Semantics;

internal static class SemanticProjectionObservation
{
    internal static bool Observes(SemanticProjection projection, SemanticId eventContract) =>
        (projection.Scope is { } scope && Observes(scope, eventContract)) ||
        projection.Transitions.Any(transition => transition.EventContract == eventContract);

    static bool Observes(SemanticProjectionScope scope, SemanticId eventContract) =>
        scope.Every is { SubscribesToAllEvents: true } ||
        scope.From.Any(from => from.EventContract == eventContract) ||
        scope.Removals.Any(removal => removal.EventContract == eventContract) ||
        scope.Joins.Any(join => join.EventContract == eventContract) ||
        scope.JoinRemovals.Any(removal => removal.EventContract == eventContract) ||
        scope.Children.Any(child => Observes(child.Scope, eventContract)) ||
        scope.Nested.Any(nested => Observes(nested.Scope, eventContract));
}
