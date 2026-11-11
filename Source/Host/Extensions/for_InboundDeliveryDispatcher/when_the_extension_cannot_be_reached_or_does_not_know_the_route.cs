// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.given;
using Cratis.Stage.Host.Extensions.Inbound;
using Xunit;

namespace Cratis.Stage.Host.Extensions.for_InboundDeliveryDispatcher;

public class when_the_extension_cannot_be_reached_or_does_not_know_the_route : an_inbound_front
{
    InboundDelivery _noBackend = null!;
    InboundDelivery _noRoute = null!;

    async Task Establish() => await StartFront();

    async Task Because()
    {
        await _deliveries.Receive(Stored("Mail", "/webhook"));
        await _deliveries.Receive(Stored("Slack", "/elsewhere"));
        await _dispatcher.RunDue(CancellationToken.None);
        _noBackend = (await _deliveries.Get(InboundDeliveryRules.Identify("Mail", "POST", "/webhook", string.Empty, []))) !;
        _noRoute = (await _deliveries.Get(InboundDeliveryRules.Identify("Slack", "POST", "/elsewhere", string.Empty, []))) !;
    }

    static InboundDelivery Stored(string system, string path) =>
        new(InboundDeliveryRules.Identify(system, "POST", path, string.Empty, []), system, "POST", path, string.Empty, [], [], DateTimeOffset.UtcNow);

    [Fact] void should_retry_when_no_backend_is_known_for_the_system() => (_noBackend.Status == InboundStatus.Retrying && _noBackend.Reason.Contains("No backend", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_reject_a_path_the_extension_declares_no_route_for() => (_noRoute.Status == InboundStatus.Rejected && _noRoute.Reason.Contains("/elsewhere", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_call_the_extension_for_a_path_it_does_not_declare() => _backend.HttpRequests.ShouldBeEmpty();
}
