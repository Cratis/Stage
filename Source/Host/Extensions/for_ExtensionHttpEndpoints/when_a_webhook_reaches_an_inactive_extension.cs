// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.given;
using Cratis.Stage.Host.Extensions.Inbound;
using Xunit;

namespace Cratis.Stage.Host.Extensions.for_ExtensionHttpEndpoints;

public class when_a_webhook_reaches_an_inactive_extension : an_inbound_front
{
    HttpResponseMessage _acknowledged = null!;
    HttpResponseMessage _replayed = null!;
    int _handshakesAtAcknowledgement;
    int _requestsAtAcknowledgement;
    InboundDelivery _stored = null!;
    int _attempts;
    InboundDelivery _delivered = null!;

    async Task Establish() => await StartFront();

    async Task Because()
    {
        _acknowledged = await Post("payload");
        _handshakesAtAcknowledgement = _backend.Handshakes;
        _requestsAtAcknowledgement = _backend.HttpRequests.Count;
        _stored = _deliveries.All.Single();

        _replayed = await Post("payload");
        _attempts = await _dispatcher.RunDue(CancellationToken.None);
        _delivered = (await _deliveries.Get(_stored.Id))!;
    }

    async Task<HttpResponseMessage> Post(string body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/extensions/Slack/webhook/events?x=1") { Content = new StringContent(body, Encoding.UTF8, "text/plain") };
        request.Headers.Add("X-Signature", "t=1,v1=abc");
        request.Headers.Add("Cookie", "session=secret");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "stage-user-token");
        return await _client.SendAsync(request);
    }

    [Fact] void should_acknowledge_the_sender_at_once() => _acknowledged.StatusCode.ShouldEqual(HttpStatusCode.Accepted);
    [Fact] void should_not_have_started_the_extension_to_acknowledge() => (_handshakesAtAcknowledgement == 0 && _requestsAtAcknowledgement == 0).ShouldBeTrue();
    [Fact] void should_keep_the_raw_body() => Encoding.UTF8.GetString(_stored.Body).ShouldEqual("payload");
    [Fact] void should_keep_the_method_the_path_and_the_query() => (_stored.Method == "POST" && _stored.Path == "/webhook/events" && _stored.Query == "x=1").ShouldBeTrue();
    [Fact] void should_keep_the_headers_a_webhook_signs() => _stored.Headers.Single(header => header.Name == "X-Signature").Values.Single().ShouldEqual("t=1,v1=abc");
    [Fact] void should_never_keep_cookies_or_the_credentials_of_the_host() => _stored.Headers.Any(header => string.Equals(header.Name, "Cookie", StringComparison.OrdinalIgnoreCase) || string.Equals(header.Name, "Authorization", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
    [Fact] void should_acknowledge_a_replay_too() => _replayed.StatusCode.ShouldEqual(HttpStatusCode.Accepted);
    [Fact] void should_store_a_replay_only_once() => _deliveries.All.Count.ShouldEqual(1);
    [Fact] void should_count_the_replay() => _delivered.Replays.ShouldEqual(1);
    [Fact] void should_attempt_the_delivery_once() => _attempts.ShouldEqual(1);
    [Fact] void should_activate_the_extension_to_deliver() => _backend.Handshakes.ShouldEqual(1);
    [Fact] void should_deliver_the_request_to_the_extension_once() => _backend.HttpRequests.Count.ShouldEqual(1);
    [Fact] void should_deliver_the_raw_body_so_a_signature_can_be_verified() => _backend.HttpRequests[0].Body.ToStringUtf8().ShouldEqual("payload");
    [Fact] void should_deliver_the_signature_header() => _backend.HttpRequests[0].Headers.Single(header => header.Name == "X-Signature").Values.Single().ShouldEqual("t=1,v1=abc");
    [Fact] void should_name_the_delivery_so_the_extension_can_deduplicate() => (_backend.HttpRequests[0].Context.InvocationId == _stored.Id && _backend.HttpRequests[0].Context.IdempotencyKey == _stored.Id && _backend.HttpRequests[0].Context.Attempt == 1).ShouldBeTrue();
    [Fact] void should_mark_it_delivered() => (_delivered.Status == InboundStatus.Delivered && _delivered.Completed is not null).ShouldBeTrue();
}
