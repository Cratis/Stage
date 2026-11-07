// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Projections;

/// <summary>
/// Refuses specification histories that expose Chronicle's event-source-keyed join backfill.
/// </summary>
internal static class SemanticKeyedRootJoinOrdering
{
    const string Reason = "A keyed root join whose key differs from its event source is followed by a root From event or command. Chronicle backfills by event source id while Screenplay backfills by key (https://github.com/Cratis/Chronicle/issues/5439).";

    /// <summary>
    /// Checks every root scope for a later From event after a differently keyed join fact.
    /// </summary>
    /// <param name="projections">The application's projections.</param>
    /// <param name="specification">The specification history to check.</param>
    /// <returns>The refusal reason, or null when backfill cannot diverge.</returns>
    internal static string? Divergence(IEnumerable<SemanticProjection> projections, SemanticSpecification specification) =>
        projections.Any(projection => projection.Scope is { } scope && Diverges(scope, specification)) ? Reason : null;

    static bool Diverges(SemanticProjectionScope scope, SemanticSpecification specification)
    {
        var differentlyKeyedJoin = false;
        foreach (var fact in specification.GivenEvents)
        {
            if (differentlyKeyedJoin && scope.From.Any(from => from.EventContract == fact.EventContract)) return true;
            differentlyKeyedJoin |= scope.Joins.Any(join => Differs(join, fact));
        }

        return differentlyKeyedJoin && (specification.When is not null ||
            (specification.WhenAppended is { } appended && scope.From.Any(from => from.EventContract == appended.EventContract)));
    }

    static bool Differs(SemanticProjectionJoin join, SemanticSpecificationEvent fact) =>
        join.EventContract == fact.EventContract &&
        join.Key is SemanticProjectionValueKey { Value: SemanticProjectionEventProperty { Path: [var id] } } &&
        (fact.Values.SingleOrDefault(value => value.TargetProperty == id)?.Value is not { } key ||
         key is SemanticNullValue || fact.EventSource is null || !Equals(key, fact.EventSource.Value));
}
