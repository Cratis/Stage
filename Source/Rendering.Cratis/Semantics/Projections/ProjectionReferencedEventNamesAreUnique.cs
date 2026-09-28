// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Projections;

/// <summary>
/// Keeps render and per-run admission aligned for every event referenced by a projection.
/// </summary>
internal static class ProjectionReferencedEventNamesAreUnique
{
    internal static bool Check(SemanticProjection projection, IReadOnlyDictionary<SemanticId, SemanticEventContract> events)
    {
        var contracts = projection.Scope is { } scoped
            ? scoped.From.Select(from => from.EventContract).Concat(scoped.Joins.Select(join => join.EventContract))
                .Concat(scoped.Removals.Select(removal => removal.EventContract))
                .Concat(scoped.JoinRemovals.Select(removal => removal.EventContract))
            : projection.Transitions.Select(transition => transition.EventContract);
        return contracts.All(id => !events.TryGetValue(id, out var @event) ||
            GeneratedPascalCase.NamesAreUnique(@event.Properties.Select(property => property.Name)));
    }
}
