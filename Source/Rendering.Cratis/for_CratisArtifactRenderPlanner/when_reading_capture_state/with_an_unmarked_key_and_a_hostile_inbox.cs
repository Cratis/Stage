// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.EventSequences;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_reading_capture_state;

public class with_an_unmarked_key_and_a_hostile_inbox : given.a_captured_delivery
{
    EventForEventSourceId[] _key_filtered = [];
    EventForEventSourceId[] _prefix = [];
    EventForEventSourceId[] _late = [];
    EventForEventSourceId[] _early = [];

    async Task Because()
    {
        Render();

        _history.Clear();
        _history.AddRange([Fact(1, "sent", "a"), Fact(2, "moved", "b", "other")]);
        _key_filtered = await Effects(Fact(3, "sent", "c"));

        _history.Clear();
        _history.AddRange([Fact(4, "moved", "e"), Fact(3, "moved", "c"), Fact(1, "sent", "a"), Fact(0, "moved", "d")]);
        _prefix = await Effects(Fact(3, "moved", "c"));

        _history.Clear();
        _history.AddRange([Fact(3, "sent", "a"), Fact(5, "sent", "b")]);
        _late = await Effects(Fact(5, "sent", "b"));
        _early = await Effects(Fact(3, "sent", "a"));
    }

    [Fact] void should_emit_nothing_when_only_another_key_changed() => _key_filtered.Length.ShouldEqual(0);
    [Fact] void should_emit_two_effects_for_the_strict_prefix() => _prefix.Length.ShouldEqual(2);
    [Fact] void should_emit_the_expected_types_after_the_strict_prefix() => _prefix.Select(effect => effect.Event.GetType().Name).ShouldEqual(["Added", "StatusRecorded"]);
    [Fact] void should_route_the_strict_prefix_effects_to_the_delivered_key() => _prefix.Select(effect => effect.EventSourceId.Value).ShouldEqual(["parcel", "parcel"]);
    [Fact] void should_tag_added_after_the_strict_prefix() => _prefix[0].Tags.ShouldContain("captured");
    [Fact] void should_tag_status_recorded_after_the_strict_prefix() => _prefix[1].Tags.ShouldContain("captured");
    [Fact] void should_record_the_delivered_id_after_the_strict_prefix() => _prefix[1].Event.GetType().GetProperty("Id")!.GetValue(_prefix[1].Event).ShouldEqual("parcel");
    [Fact] void should_record_the_delivered_status_after_the_strict_prefix() => _prefix[1].Event.GetType().GetProperty("Status")!.GetValue(_prefix[1].Event).ShouldEqual("moved");
    [Fact] void should_emit_nothing_for_the_later_event_delivered_first() => _late.Length.ShouldEqual(0);
    [Fact] void should_emit_two_effects_for_the_earlier_event_delivered_second() => _early.Length.ShouldEqual(2);
    [Fact] void should_emit_the_expected_types_for_the_earlier_event() => _early.Select(effect => effect.Event.GetType().Name).ShouldEqual(["Added", "StatusRecorded"]);
    [Fact] void should_route_the_earlier_effects_to_the_delivered_key() => _early.Select(effect => effect.EventSourceId.Value).ShouldEqual(["parcel", "parcel"]);
    [Fact] void should_tag_added_for_the_earlier_event() => _early[0].Tags.ShouldContain("captured");
    [Fact] void should_tag_status_recorded_for_the_earlier_event() => _early[1].Tags.ShouldContain("captured");
    [Fact] void should_record_the_delivered_id_for_the_earlier_event() => _early[1].Event.GetType().GetProperty("Id")!.GetValue(_early[1].Event).ShouldEqual("parcel");
    [Fact] void should_record_sent_for_the_earlier_event() => _early[1].Event.GetType().GetProperty("Status")!.GetValue(_early[1].Event).ShouldEqual("sent");
}
#endif
