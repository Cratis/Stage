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
        return Contracts(projection).All(id => !events.TryGetValue(id, out var @event) ||
            GeneratedPascalCase.EventMembersAreUnique(@event.Name, @event.Properties.Select(property => property.Name)));
    }

    internal static IEnumerable<SemanticId> Contracts(SemanticProjection projection) => projection.Scope is { } scope
        ? ScopeEvents(scope).Distinct()
        : projection.Transitions.Select(transition => transition.EventContract);

    internal static IEnumerable<SemanticId> ScopeEvents(SemanticProjectionScope scope) =>
        scope.From.Select(_ => _.EventContract)
            .Concat(scope.Joins.Select(_ => _.EventContract))
            .Concat(scope.Removals.Select(_ => _.EventContract))
            .Concat(scope.JoinRemovals.Select(_ => _.EventContract))
            .Concat(scope.Children.SelectMany(_ => ScopeEvents(_.Scope)))
            .Concat(scope.Nested.SelectMany(_ => ScopeEvents(_.Scope)));
}
