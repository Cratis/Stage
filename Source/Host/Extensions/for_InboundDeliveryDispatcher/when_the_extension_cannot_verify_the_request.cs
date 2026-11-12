// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Extensions.Protocol;
using Cratis.Stage.Host.Extensions.given;
using Cratis.Stage.Host.Extensions.Inbound;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.Extensions.for_InboundDeliveryDispatcher;

public class when_the_extension_cannot_verify_the_request : an_inbound_front
{
    InboundDelivery _after = null!;
    int _firstRound;
    int _secondRound;

    async Task Establish()
    {
        await StartFront();
        _backend.OnHttp = _ => new InboundHttpResponse { Status = 401 };
    }

    async Task Because()
    {
        await _client.PostAsync("/extensions/Slack/webhook", new StringContent("forged", Encoding.UTF8, "text/plain"));
        _firstRound = await _dispatcher.RunDue(CancellationToken.None);
        _time.Advance(TimeSpan.FromDays(1));
        _secondRound = await _dispatcher.RunDue(CancellationToken.None);
        _after = _deliveries.All.Single();
    }

    [Fact] void should_try_it_once() => (_firstRound == 1 && _backend.HttpRequests.Count == 1).ShouldBeTrue();
    [Fact] void should_reject_it() => (_after.Status == InboundStatus.Rejected && _after.ResponseStatus == 401).ShouldBeTrue();
    [Fact] void should_never_try_the_same_request_again() => _secondRound.ShouldEqual(0);
    [Fact] void should_raise_nothing() => _triggers.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_keep_it_so_an_operator_can_see_it() => _deliveries.All.Count.ShouldEqual(1);
}
