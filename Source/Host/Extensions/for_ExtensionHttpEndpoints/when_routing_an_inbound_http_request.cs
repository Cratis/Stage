// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Extensions.Protocol;
using Cratis.Stage.Host.Extensions.given;
using Google.Protobuf;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Xunit;

namespace Cratis.Stage.Host.Extensions.for_ExtensionHttpEndpoints;

public class when_routing_an_inbound_http_request : an_extension_protocol
{
    WebApplication _front = null!;
    HttpClient _client = null!;
    HttpResponseMessage _handled = null!;
    string _handledBody = null!;
    HttpStatusCode _undeclaredPath;
    HttpStatusCode _unknownSystem;
    HttpStatusCode _outOfRange;
    HttpStatusCode _tooLarge;
    HttpStatusCode _prefixLookalike;

    async Task Establish()
    {
        await StartBackend();
        await StartStage();
        _backend.Description.HttpRoutes.Add("/webhook");
        _backend.OnHttp = request => request.Path == "/webhook/bad"
            ? new InboundHttpResponse { Status = 999 }
            : new InboundHttpResponse { Status = 202, Body = ByteString.CopyFromUtf8("got it"), Headers = { new Header { Name = "X-Reply", Values = { "ok" } }, new Header { Name = "Set-Cookie", Values = { "a=b" } } } };

        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseKestrelCore().ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, 0, listen => listen.Protocols = HttpProtocols.Http1));
        builder.Services.AddSingleton(_connections);
        builder.Services.AddRouting();
        _front = builder.Build();
        _front.UseRouting();
        ExtensionHttpEndpoints.Map(_front);
        await _front.StartAsync();
        _client = new HttpClient { BaseAddress = new Uri(_front.Urls.First()) };
    }

    async Task Because()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/extensions/Slack/webhook/events?x=1") { Content = new StringContent("payload", Encoding.UTF8, "text/plain") };
        request.Headers.Add("X-Signature", "abc");
        request.Headers.Add("Cookie", "session=secret");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "stage-user-token");
        _handled = await _client.SendAsync(request);
        _handledBody = await _handled.Content.ReadAsStringAsync();

        _undeclaredPath = (await _client.GetAsync("/extensions/Slack/other")).StatusCode;
        _prefixLookalike = (await _client.GetAsync("/extensions/Slack/webhooks")).StatusCode;
        _unknownSystem = (await _client.GetAsync("/extensions/Mail/webhook")).StatusCode;
        _outOfRange = (await _client.GetAsync("/extensions/Slack/webhook/bad")).StatusCode;
        _tooLarge = (await _client.PostAsync("/extensions/Slack/webhook", new ByteArrayContent(new byte[ExtensionHttpEndpoints.MaxBodyBytes + 1]))).StatusCode;
    }

    async Task Destroy()
    {
        _client.Dispose();
        await _front.StopAsync();
        await _front.DisposeAsync();
    }

    [Fact] void should_answer_what_the_backend_answered() => (_handled.StatusCode == HttpStatusCode.Accepted && _handledBody == "got it").ShouldBeTrue();
    [Fact] void should_return_the_headers_the_backend_set() => _handled.Headers.GetValues("X-Reply").Single().ShouldEqual("ok");
    [Fact] void should_not_return_cookies_for_the_host_s_origin() => _handled.Headers.Contains("Set-Cookie").ShouldBeFalse();
    [Fact] void should_forward_the_method_the_path_and_the_query() => (_backend.HttpRequests[0].Method == "POST" && _backend.HttpRequests[0].Path == "/webhook/events" && _backend.HttpRequests[0].Query == "x=1").ShouldBeTrue();
    [Fact] void should_forward_the_body() => _backend.HttpRequests[0].Body.ToStringUtf8().ShouldEqual("payload");
    [Fact] void should_forward_the_headers_a_webhook_signs() => _backend.HttpRequests[0].Headers.Single(header => header.Name == "X-Signature").Values.Single().ShouldEqual("abc");
    [Fact] void should_not_forward_the_credentials_of_the_host() => _backend.HttpRequests[0].Headers.Any(header => string.Equals(header.Name, "Cookie", StringComparison.Ordinal) || string.Equals(header.Name, "Authorization", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_name_the_system_and_the_tenant() => (_backend.HttpRequests[0].Context.System == "Slack" && _backend.HttpRequests[0].Context.Tenant == "Default").ShouldBeTrue();
    [Fact] void should_not_route_a_path_the_backend_does_not_handle() => _undeclaredPath.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_not_route_a_path_that_only_starts_like_a_route() => _prefixLookalike.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_not_route_to_a_system_with_no_backend() => _unknownSystem.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_answer_bad_gateway_for_a_status_that_is_not_http() => _outOfRange.ShouldEqual(HttpStatusCode.BadGateway);
    [Fact] void should_refuse_a_body_that_is_too_large() => _tooLarge.ShouldEqual(HttpStatusCode.RequestEntityTooLarge);
}
