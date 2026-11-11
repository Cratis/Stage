// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Extensions.Protocol;
using Cratis.Stage.Host.Extensions.given;
using Cratis.Stage.Host.Extensions.Inbound;
using Xunit;

namespace Cratis.Stage.Host.Extensions.for_InboundDeliveryDispatcher;

public class when_every_attempt_fails : an_inbound_front
{
    InboundDelivery _failed = null!;
    int _afterwards;
    bool _requeued;
    InboundDelivery _requeuedDelivery = null!;
    IReadOnlyList<InboundDelivery> _listed = null!;

    async Task Establish()
    {
        await StartFront();
        _options.Inbound.MaxAttempts = 3;
        _backend.OnHttp = _ => new InboundHttpResponse { Status = 500 };
    }

    async Task Because()
    {
        await _client.PostAsync("/extensions/Slack/webhook", new StringContent("event", Encoding.UTF8, "text/plain"));
        for (var round = 0; round < 3; round++)
        {
            await _dispatcher.RunDue(CancellationToken.None);
            _time.Advance(_options.Inbound.MaxRetryDelay);
        }

        _failed = _deliveries.All.Single();
        _afterwards = await _dispatcher.RunDue(CancellationToken.None);
        _listed = await _deliveries.List(InboundStatus.Failed);
        _requeued = await _deliveries.Requeue(_failed.Id);
        _requeuedDelivery = _deliveries.All.Single();
    }

    [Fact] void should_give_up_after_the_attempts_allowed() => (_failed.Status == InboundStatus.Failed && _failed.Attempts == 3).ShouldBeTrue();
    [Fact] void should_stop_trying() => _afterwards.ShouldEqual(0);
    [Fact] void should_show_it_as_failed_in_the_workbench() => _listed.Single().Id.ShouldEqual(_failed.Id);
    [Fact] void should_let_an_operator_retry_it() => (_requeued && _requeuedDelivery.Status == InboundStatus.Pending && _requeuedDelivery.Attempts == 0).ShouldBeTrue();
}
