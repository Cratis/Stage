// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Extensions.for_WebhookSignature;

public class when_verifying_a_timestamped_signature : Specification
{
    const string Secret = "whsec_test";
    static readonly byte[] _body = Encoding.UTF8.GetBytes("{\"id\":\"evt_1\"}");
    static readonly DateTimeOffset _now = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);
    static readonly TimeSpan _tolerance = TimeSpan.FromMinutes(5);

    long _sentAt;
    string _signature = null!;
    bool _fresh;
    bool _atTheEdge;
    bool _replayedLater;
    bool _fromTheFuture;
    bool _timestampMoved;
    bool _bodyOnlySignature;
    bool _negativeTolerance;

    static string Sign(long timestamp, byte[] body) => WebhookSignature.Compute(Secret, Encoding.UTF8.GetBytes($"{timestamp}." + Encoding.UTF8.GetString(body)));

    void Because()
    {
        _sentAt = _now.ToUnixTimeSeconds();
        _signature = Sign(_sentAt, _body);
        _fresh = WebhookSignature.Verify(Secret, _body, _sentAt, _signature, _now, _tolerance);
        _atTheEdge = WebhookSignature.Verify(Secret, _body, _sentAt, _signature, _now + _tolerance, _tolerance);
        _replayedLater = WebhookSignature.Verify(Secret, _body, _sentAt, _signature, _now + _tolerance + TimeSpan.FromSeconds(1), _tolerance);
        _fromTheFuture = WebhookSignature.Verify(Secret, _body, _sentAt, _signature, _now - _tolerance - TimeSpan.FromSeconds(1), _tolerance);
        _timestampMoved = WebhookSignature.Verify(Secret, _body, _sentAt + 3600, _signature, _now + TimeSpan.FromHours(1), _tolerance);
        _bodyOnlySignature = WebhookSignature.Verify(Secret, _body, _sentAt, WebhookSignature.Compute(Secret, _body), _now, _tolerance);
        _negativeTolerance = WebhookSignature.Verify(Secret, _body, _sentAt, _signature, _now, TimeSpan.FromSeconds(-1));
    }

    [Fact] void should_accept_a_fresh_request() => _fresh.ShouldBeTrue();
    [Fact] void should_accept_a_request_at_the_edge_of_the_tolerance() => _atTheEdge.ShouldBeTrue();
    [Fact] void should_refuse_a_captured_request_replayed_after_the_tolerance() => _replayedLater.ShouldBeFalse();
    [Fact] void should_refuse_a_timestamp_too_far_ahead() => _fromTheFuture.ShouldBeFalse();
    [Fact] void should_refuse_a_timestamp_that_was_moved_forward_to_beat_the_tolerance() => _timestampMoved.ShouldBeFalse();
    [Fact] void should_refuse_a_signature_that_does_not_cover_the_timestamp() => _bodyOnlySignature.ShouldBeFalse();
    [Fact] void should_refuse_a_negative_tolerance() => _negativeTolerance.ShouldBeFalse();
}
