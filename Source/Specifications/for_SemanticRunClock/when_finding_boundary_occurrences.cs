// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticRunClock;

public class when_finding_boundary_occurrences : Specification
{
    static readonly SemanticId ReactionId = SemanticId.Parse("sem1:3a829ffd770e18dc85cc1ecca5c121cf437ad54d97113a1edc6983dccc6903a9");
    DateTimeOffset[] _fractional;
    DateTimeOffset[] _beforeEpoch;
    DateTimeOffset[] _offset;
    DateTimeOffset[] _months;
    DateTimeOffset[] _lastDay;
    DateTimeOffset[] _largeInterval;
    bool _withinLimit;
    bool _overLimit;

    void Because()
    {
        var interval = new SemanticReactionTrigger(SemanticReactionTriggerKind.Interval) { Every = 5 };
        _fractional = Due(interval, "1969-12-31T23:59:59.500Z", "1970-01-01T00:00:05Z");
        _beforeEpoch = Due(interval, "1969-12-31T23:59:49Z", "1969-12-31T23:59:59Z");
        _offset = Due(new(SemanticReactionTriggerKind.Schedule) { At = 0 }, "2026-10-01T23:59:59Z", "2026-10-02T02:00:00+02:00");
        _months = Due(new(SemanticReactionTriggerKind.Schedule) { At = 0, OnDayOfMonth = 31 }, "2026-01-30T00:00:00Z", "2026-03-31T00:00:00Z");
        _lastDay = Due(new(SemanticReactionTriggerKind.Schedule) { At = 0 }, "9999-12-30T23:59:59Z", "9999-12-31T23:59:59Z");
        _largeInterval = Due(new(SemanticReactionTriggerKind.Interval) { Every = long.MaxValue }, "1969-12-31T23:59:59Z", "9999-12-31T23:59:59Z");
        var seconds = new SemanticReaction(ReactionId, "Seconds", [new(SemanticReactionTriggerKind.Interval) { Every = 1 }]);
        _withinLimit = SemanticRunClock.TryFindDue([seconds], DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddSeconds(SemanticRunClock.MaximumOccurrences), out _);
        _overLimit = SemanticRunClock.TryFindDue([seconds], DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddSeconds(SemanticRunClock.MaximumOccurrences + 1), out _);
    }

    [Fact] void should_include_epoch_and_exact_endpoint_after_a_fractional_start() => _fractional.ShouldContainOnly(DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddSeconds(5));
    [Fact] void should_floor_negative_intervals() => _beforeEpoch.ShouldContainOnly(DateTimeOffset.UnixEpoch.AddSeconds(-10), DateTimeOffset.UnixEpoch.AddSeconds(-5));
    [Fact] void should_normalize_offset_times_to_utc() => _offset.ShouldContainOnly(Instant("2026-10-02T00:00:00Z"));
    [Fact] void should_skip_months_without_the_requested_day() => _months.ShouldContainOnly(Instant("2026-01-31T00:00:00Z"), Instant("2026-03-31T00:00:00Z"));
    [Fact] void should_stop_without_overflow_at_the_last_calendar_day() => _lastDay.ShouldContainOnly(Instant("9999-12-31T00:00:00Z"));
    [Fact] void should_handle_an_interval_larger_than_the_calendar_without_overflow() => _largeInterval.ShouldContainOnly(DateTimeOffset.UnixEpoch);
    [Fact] void should_admit_the_exact_occurrence_limit() => _withinLimit.ShouldBeTrue();
    [Fact] void should_refuse_one_more_occurrence() => _overLimit.ShouldBeFalse();

    [Fact]
    void should_count_simultaneous_triggers_toward_the_shared_occurrence_limit()
    {
        var seconds = new SemanticReaction(ReactionId, "Simultaneous", [new(SemanticReactionTriggerKind.Interval) { Every = 1 }, new(SemanticReactionTriggerKind.Interval) { Every = 1 }]);
        SemanticRunClock.TryFindDue([seconds], DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddSeconds(SemanticRunClock.MaximumOccurrences / 2), out var due).ShouldBeTrue();
        due.Count.ShouldEqual(SemanticRunClock.MaximumOccurrences);
        SemanticRunClock.TryFindDue([seconds], DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddSeconds((SemanticRunClock.MaximumOccurrences / 2) + 1), out _).ShouldBeFalse();
    }

    static DateTimeOffset[] Due(SemanticReactionTrigger trigger, string from, string to)
    {
        SemanticRunClock.TryFindDue([new(ReactionId, "Boundary", [trigger])], Instant(from), Instant(to), out var due).ShouldBeTrue();

        return [.. due.Select(value => value.At)];
    }

    static DateTimeOffset Instant(string value) => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
}
