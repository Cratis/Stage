// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Inbound;
using Xunit;

namespace Cratis.Stage.Host.Extensions.for_InboundDeliveryRules;

public class when_settling_an_attempt : Specification
{
    static readonly DateTimeOffset _now = new(2030, 1, 2, 0, 0, 0, TimeSpan.Zero);

    InboundOptions _options = null!;
    InboundDelivery _fresh = null!;
    InboundDelivery _delivered = null!;
    InboundDelivery _rejected = null!;
    InboundDelivery _retrying = null!;
    InboundDelivery _secondFailure = null!;
    InboundDelivery _exhausted = null!;

    void Establish()
    {
        _options = new InboundOptions { MaxAttempts = 3, RetryDelay = TimeSpan.FromSeconds(5), MaxRetryDelay = TimeSpan.FromSeconds(8) };
        _fresh = new("Payments/abc", "Payments", "POST", "/webhook", string.Empty, [], [], _now);
    }

    void Because()
    {
        _delivered = InboundDeliveryRules.Settle(_fresh, InboundOutcome.Delivered, 200, "ok", _now, _options);
        _rejected = InboundDeliveryRules.Settle(_fresh, InboundOutcome.Rejected, 401, "bad signature", _now, _options);
        _retrying = InboundDeliveryRules.Settle(_fresh, InboundOutcome.Retry, 500, "boom", _now, _options);
        _secondFailure = InboundDeliveryRules.Settle(_retrying, InboundOutcome.Retry, 500, "boom", _now, _options);
        _exhausted = InboundDeliveryRules.Settle(_secondFailure, InboundOutcome.Retry, 500, "boom", _now, _options);
    }

    [Fact] void should_deliver_on_success() => (_delivered.Status == InboundStatus.Delivered && _delivered.Completed == _now && !_delivered.IsOpen).ShouldBeTrue();
    [Fact] void should_reject_for_good_when_the_extension_refuses() => (_rejected.Status == InboundStatus.Rejected && !_rejected.IsOpen && _rejected.ResponseStatus == 401).ShouldBeTrue();
    [Fact] void should_count_the_attempt() => _retrying.Attempts.ShouldEqual(1);
    [Fact] void should_retry_after_the_base_delay() => (_retrying.Status == InboundStatus.Retrying && _retrying.NextAttempt == _now + TimeSpan.FromSeconds(5)).ShouldBeTrue();
    [Fact] void should_double_the_wait_for_each_failure_up_to_the_maximum() => _secondFailure.NextAttempt.ShouldEqual(_now + TimeSpan.FromSeconds(8));
    [Fact] void should_give_up_after_the_last_attempt() => (_exhausted.Status == InboundStatus.Failed && _exhausted.Attempts == 3 && _exhausted.Completed == _now).ShouldBeTrue();
    [Fact] void should_keep_what_happened() => _exhausted.Reason.ShouldEqual("boom");
}
