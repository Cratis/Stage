// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Pairs expected facts with their production occurrences for a projection scenario.
/// </summary>
internal static class SemanticProjectionSpecificationEvents
{
    /// <summary>
    /// Gets the facts to replay in production order. Admission rejects ambiguous unordered duplicates.
    /// </summary>
    /// <param name="specification">The admitted specification.</param>
    /// <param name="projection">The projected read model.</param>
    /// <param name="command">The command producing the facts.</param>
    /// <returns>The produced and expected occurrences in replay order.</returns>
    internal static IReadOnlyList<(SemanticProducedEvent Produced, SemanticSpecificationEvent Expected)> Replay(
        SemanticSpecification specification,
        SemanticProjection projection,
        SemanticCommand command)
    {
        if (projection.Scope is null)
        {
            var produced = command.Produces.Single(_ => _.EventContract == projection.Transitions.Single().EventContract);
            var expected = specification.ThenEvents.Single(_ => _.EventContract == produced.EventContract);
            return [(produced, expected)];
        }

        return [.. command.Produces.Select((produced, index) => (produced,
            specification.ThenEventsInAnyOrder
                ? specification.ThenEvents.Single(_ => _.EventContract == produced.EventContract)
                : specification.ThenEvents[index]))];
    }
}
