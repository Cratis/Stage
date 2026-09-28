// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Limits Chronicle's multi-source scenario replay to the reducer's single event-source key.</summary>
internal static class SemanticReducerSpecificationEvents
{
    internal static IReadOnlyList<SemanticSpecificationEvent> Given(
        SemanticSpecification specification,
        SemanticSpecificationReadModel expected,
        SemanticReducer reducer) =>
        [.. specification.GivenEvents.Where(given => reducer.Transitions.Any(_ => _.EventContract == given.EventContract) &&
            Equals(given.EventSource?.Value, expected.Key))];

    internal static IReadOnlyList<(SemanticProducedEvent Produced, SemanticSpecificationEvent Expected)> Replay(
        SemanticSpecification specification,
        SemanticSpecificationReadModel expected,
        SemanticReducer reducer,
        SemanticCommand command) =>
        [.. command.Produces.Select((produced, index) => (Produced: produced,
                Expected: specification.ThenEventsInAnyOrder
                    ? specification.ThenEvents.Single(_ => _.EventContract == produced.EventContract)
                    : specification.ThenEvents[index]))
            .Where(pair => reducer.Transitions.Any(_ => _.EventContract == pair.Produced.EventContract) &&
                Equals(SemanticDestinations.ForSpecification(specification, command, pair.Produced).Value, expected.Key))];
}
