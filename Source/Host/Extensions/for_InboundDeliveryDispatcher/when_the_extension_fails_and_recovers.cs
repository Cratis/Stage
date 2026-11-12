// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Extensions.Protocol;
using Cratis.Stage.Host.Extensions.given;
using Cratis.Stage.Host.Extensions.Inbound;
using Xunit;

namespace Cratis.Stage.Host.Extensions.for_InboundDeliveryDispatcher;

public class when_the_extension_fails_and_recovers : an_inbound_front
{
    InboundDelivery _afterFailure = null!;
    int _tooEarly;
    int _onTime;
    InboundDelivery _afterRecovery = null!;
    int _calls;

    async Task Establish()
    {
        await StartFront();
        _backend.OnHttp = _ => ++_calls == 1 ? new InboundHttpResponse { Status = 503 } : new InboundHttpResponse { Status = 200 };
    }

    async Task Because()
    {
        await _client.PostAsync("/extensions/Slack/webhook", new StringContent("event", Encoding.UTF8, "text/plain"));
        await _dispatcher.RunDue(CancellationToken.None);
        _afterFailure = _deliveries.All.Single();
        _tooEarly = await _dispatcher.RunDue(CancellationToken.None);
        _time.Advance(_options.Inbound.RetryDelay);
        _onTime = await _dispatcher.RunDue(CancellationToken.None);
        _afterRecovery = _deliveries.All.Single();
    }

    [Fact] void should_retry_after_a_failure() => (_afterFailure.Status == InboundStatus.Retrying && _afterFailure.Attempts == 1).ShouldBeTrue();
    [Fact] void should_show_why_in_the_workbench() => _afterFailure.Reason.ShouldContain("503");
    [Fact] void should_wait_before_trying_again() => _tooEarly.ShouldEqual(0);
    [Fact] void should_try_again_when_the_wait_is_over() => _onTime.ShouldEqual(1);
    [Fact] void should_deliver_on_the_second_attempt() => (_afterRecovery.Status == InboundStatus.Delivered && _afterRecovery.Attempts == 2).ShouldBeTrue();
    [Fact] void should_tell_the_extension_which_attempt_it_is() => _backend.HttpRequests.Select(request => request.Context.Attempt).ToArray().ShouldEqual([1, 2]);
    [Fact] void should_keep_the_same_identity_across_attempts_so_the_extension_can_deduplicate() => _backend.HttpRequests.Select(request => request.Context.IdempotencyKey).Distinct().Count().ShouldEqual(1);
}
