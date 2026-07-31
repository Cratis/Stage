// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Commands;
using Xunit;

namespace Cratis.Stage.Runtime.for_ProducedEventPayloads.when_building;

public class and_conditions_guard_the_events : given.a_command_payload
{
    IReadOnlyList<ProducedEventPayload> _events = [];

    void Because() => _events = ProducedEventPayloads.Build(
        [
            Produced("AlwaysProduced", When: null),
            Produced("ConditionHolds", new ProducedEventComparison("isProForma", ProducedEventComparisonOperator.Equal, "true")),
            Produced("ConditionDoesNotHold", new ProducedEventComparison("currency", ProducedEventComparisonOperator.Equal, "\"NOK\"")),
            Produced("NotEqualHolds", new ProducedEventComparison("currency", ProducedEventComparisonOperator.NotEqual, "\"NOK\"")),
            Produced("GreaterThanHolds", new ProducedEventComparison("quantity", ProducedEventComparisonOperator.GreaterThan, "0")),
            Produced("GreaterThanDoesNotHold", new ProducedEventComparison("quantity", ProducedEventComparisonOperator.GreaterThan, "10")),
            Produced("EitherHolds", new ProducedEventLogicalCondition(
                new ProducedEventComparison("currency", ProducedEventComparisonOperator.Equal, "\"NOK\""),
                ProducedEventLogicalOperator.Or,
                new ProducedEventComparison("currency", ProducedEventComparisonOperator.Equal, "\"USD\""))),
            Produced("BothDoNotHold", new ProducedEventLogicalCondition(
                new ProducedEventComparison("currency", ProducedEventComparisonOperator.Equal, "\"USD\""),
                ProducedEventLogicalOperator.And,
                new ProducedEventComparison("quantity", ProducedEventComparisonOperator.GreaterThan, "10"))),
            Produced("MissingPropertyDoesNotHold", new ProducedEventComparison("notOnThePayload", ProducedEventComparisonOperator.Equal, "\"anything\"")),
        ],
        _command,
        _occurred,
        _identity);

    [Fact] void should_produce_only_the_events_whose_condition_holds() =>
        _events.Select(@event => @event.EventType).ShouldContainOnly(
            "AlwaysProduced",
            "ConditionHolds",
            "NotEqualHolds",
            "GreaterThanHolds",
            "EitherHolds");

    static ProducedEvent Produced(string name, ProducedEventCondition? When) => new(name, When, [], []);
}
