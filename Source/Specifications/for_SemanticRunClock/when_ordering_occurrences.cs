// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticRunClock;

public class when_ordering_occurrences : Specification
{
    List<(DateTimeOffset At, SemanticReaction Reaction, SemanticReactionTrigger Trigger)> _due;
    SemanticReaction _first;
    SemanticReaction _second;

    void Establish()
    {
        var id = SemanticId.Parse("sem1:3a829ffd770e18dc85cc1ecca5c121cf437ad54d97113a1edc6983dccc6903a9");
        _first = new(id, "First", [new(SemanticReactionTriggerKind.Interval) { Every = 5 }, new(SemanticReactionTriggerKind.Schedule) { At = 5 }]);
        _second = new(id, "Second", [new(SemanticReactionTriggerKind.Interval) { Every = 5 }]);
    }
    void Because() => SemanticRunClock.TryFindDue([_first, _second], DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddSeconds(10), out _due).ShouldBeTrue();
    [Fact] void should_order_by_instant_then_supplied_reaction_order() => _due.Select(value => value.Reaction.Name).ShouldEqual(["First", "First", "Second", "First", "Second"]);
    [Fact] void should_preserve_trigger_order_at_the_same_instant() => _due.Take(2).Select(value => value.Trigger.Kind).ShouldEqual([SemanticReactionTriggerKind.Interval, SemanticReactionTriggerKind.Schedule]);
}
