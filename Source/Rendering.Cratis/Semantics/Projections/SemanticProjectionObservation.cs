// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Projections;

internal static class SemanticProjectionObservation
{
    internal static bool Observes(SemanticProjection projection, SemanticId eventContract) =>
        projection.Scope?.Every is { SubscribesToAllEvents: true } ||
        projection.Scope?.From.Any(from => from.EventContract == eventContract) == true ||
        projection.Scope?.Removals.Any(removal => removal.EventContract == eventContract) == true ||
        projection.Transitions.Any(transition => transition.EventContract == eventContract);
}
