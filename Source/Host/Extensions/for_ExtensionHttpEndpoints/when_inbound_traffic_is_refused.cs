// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.for_ExtensionHttpEndpoints;

public class when_inbound_traffic_is_refused : an_inbound_front
{
    HttpStatusCode _unknownSystem;
    HttpStatusCode _tooLarge;
    HttpStatusCode _undeclaredPathOfARunningExtension;
    HttpResponseMessage _full = null!;
    int _storedBeforeFull;

    async Task Establish()
    {
        await StartFront();
        _options.Inbound.MaxOpen = 2;
    }

    async Task Because()
    {
        _unknownSystem = (await _client.PostAsync("/extensions/Mail/webhook", new StringContent("x"))).StatusCode;
        _tooLarge = (await _client.PostAsync("/extensions/Slack/webhook", new ByteArrayContent(new byte[ExtensionHttpEndpoints.MaxBodyBytes + 1]))).StatusCode;
        await _client.PostAsync("/extensions/Slack/webhook", new StringContent("one"));
        await _client.PostAsync("/extensions/Slack/webhook", new StringContent("two"));
        _storedBeforeFull = _deliveries.All.Count;
        _full = await _client.PostAsync("/extensions/Slack/webhook", new StringContent("three"));

        await _dispatcher.RunDue(CancellationToken.None);
        _undeclaredPathOfARunningExtension = (await _client.PostAsync("/extensions/Slack/other", new StringContent("x"))).StatusCode;
    }

    [Fact] void should_not_store_traffic_for_a_system_with_no_backend() => _unknownSystem.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_refuse_a_body_that_is_too_large() => _tooLarge.ShouldEqual(HttpStatusCode.RequestEntityTooLarge);
    [Fact] void should_store_nothing_for_either() => _storedBeforeFull.ShouldEqual(2);
    [Fact] void should_answer_unavailable_when_too_many_requests_wait() => _full.StatusCode.ShouldEqual(HttpStatusCode.ServiceUnavailable);
    [Fact] void should_tell_the_sender_when_to_come_back() => _full.Headers.RetryAfter!.Delta.ShouldEqual(TimeSpan.FromSeconds(60));
    [Fact] void should_not_store_the_request_it_turned_away() => _deliveries.All.Count.ShouldEqual(2);
    [Fact] void should_not_store_a_path_a_running_extension_does_not_declare() => _undeclaredPathOfARunningExtension.ShouldEqual(HttpStatusCode.NotFound);
}
