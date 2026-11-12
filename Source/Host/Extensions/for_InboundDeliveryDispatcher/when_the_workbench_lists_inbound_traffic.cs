// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Extensions.Protocol;
using Cratis.Stage.Host.Extensions.given;
using Cratis.Stage.Host.Extensions.Inbound;
using Xunit;

namespace Cratis.Stage.Host.Extensions.for_InboundDeliveryDispatcher;

public class when_the_workbench_lists_inbound_traffic : an_inbound_front
{
    JsonElement _failed;
    string _failedText = null!;
    HttpStatusCode _badStatus;
    HttpStatusCode _unknown;
    HttpStatusCode _retried;
    HttpStatusCode _notRetryable;
    InboundDelivery _afterRetry = null!;

    async Task Establish()
    {
        await StartFront();
        _options.Inbound.MaxAttempts = 1;
        _backend.OnHttp = _ => new InboundHttpResponse { Status = 500 };
    }

    async Task Because()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/extensions/Slack/webhook") { Content = new StringContent("secret-body", Encoding.UTF8, "text/plain") };
        request.Headers.Add("X-Signature", "secret-signature");
        await _client.SendAsync(request);
        await _dispatcher.RunDue(CancellationToken.None);

        _failedText = await _client.GetStringAsync("/stage/extensions/deliveries?status=failed");
        _failed = JsonSerializer.Deserialize<JsonElement>(_failedText);
        _badStatus = (await _client.GetAsync("/stage/extensions/deliveries?status=sleeping")).StatusCode;
        var id = _deliveries.All.Single().Id;
        _unknown = (await _client.PostAsJsonAsync("/stage/extensions/deliveries/retry", new { id = "Slack/nothing" })).StatusCode;
        _retried = (await _client.PostAsJsonAsync("/stage/extensions/deliveries/retry", new { id })).StatusCode;
        _afterRetry = _deliveries.All.Single();
        _notRetryable = (await _client.PostAsJsonAsync("/stage/extensions/deliveries/retry", new { id })).StatusCode;
    }

    [Fact] void should_list_the_failed_delivery() => (_failed.GetProperty("deliveries").GetArrayLength() == 1 && _failed.GetProperty("failed").GetInt32() == 1).ShouldBeTrue();
    [Fact] void should_show_what_became_of_it() => (_failed.GetProperty("deliveries")[0].GetProperty("status").GetString() == "Failed" && _failed.GetProperty("deliveries")[0].GetProperty("responseStatus").GetInt32() == 500).ShouldBeTrue();
    [Fact] void should_show_the_size_of_the_body() => _failed.GetProperty("deliveries")[0].GetProperty("bodyBytes").GetInt32().ShouldEqual("secret-body".Length);
    [Fact] void should_never_show_the_body() => _failedText.Contains("secret-body", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_never_show_the_value_of_a_header() => _failedText.Contains("secret-signature", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_refuse_a_status_that_does_not_exist() => _badStatus.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_not_retry_what_is_unknown() => _unknown.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_retry_a_failed_delivery() => (_retried == HttpStatusCode.Accepted && _afterRetry.Status == InboundStatus.Pending).ShouldBeTrue();
    [Fact] void should_not_retry_a_delivery_that_is_waiting_already() => _notRetryable.ShouldEqual(HttpStatusCode.Conflict);
}
