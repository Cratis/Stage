// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Inbound;
using Xunit;

namespace Cratis.Stage.Host.Extensions.for_InboundDeliveryRules;

public class when_deciding_whether_a_request_is_a_replay : Specification
{
    static readonly DateTimeOffset _now = new(2030, 1, 2, 0, 0, 0, TimeSpan.Zero);
    static readonly TimeSpan _window = TimeSpan.FromHours(24);

    static InboundDelivery Stored(InboundStatus status, TimeSpan age) =>
        new("Payments/abc", "Payments", "POST", "/webhook", string.Empty, [], [], _now - age) { Status = status };

    [Fact] void should_not_be_a_replay_when_nothing_is_stored() => InboundDeliveryRules.IsReplay(null, _now, _window).ShouldBeFalse();
    [Fact] void should_be_a_replay_while_the_stored_request_waits_however_old() => InboundDeliveryRules.IsReplay(Stored(InboundStatus.Retrying, TimeSpan.FromDays(3)), _now, _window).ShouldBeTrue();
    [Fact] void should_be_a_replay_of_a_delivered_request_within_the_window() => InboundDeliveryRules.IsReplay(Stored(InboundStatus.Delivered, TimeSpan.FromHours(1)), _now, _window).ShouldBeTrue();
    [Fact] void should_be_a_replay_of_a_rejected_request_within_the_window() => InboundDeliveryRules.IsReplay(Stored(InboundStatus.Rejected, TimeSpan.FromHours(1)), _now, _window).ShouldBeTrue();
    [Fact] void should_be_new_once_the_window_has_passed() => InboundDeliveryRules.IsReplay(Stored(InboundStatus.Delivered, TimeSpan.FromHours(25)), _now, _window).ShouldBeFalse();
    [Fact] void should_be_new_when_every_attempt_failed_so_a_senders_retry_is_delivered() => InboundDeliveryRules.IsReplay(Stored(InboundStatus.Failed, TimeSpan.FromHours(1)), _now, _window).ShouldBeFalse();
}
