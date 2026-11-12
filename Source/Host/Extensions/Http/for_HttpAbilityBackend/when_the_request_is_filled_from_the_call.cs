// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Http.given;
using Cratis.Stage.Host.Invocations;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Http.for_HttpAbilityBackend;

public class when_the_request_is_filled_from_the_call : an_http_system
{
    const string Block = """
        get "/parcels/{parcelId}/events"
        query "since" = since
        query "kind" = "pickup"
        query "attempt" = $call.attempt
        response 200
          status = status
          count = body.items.count
        """;

    AbilityResult _result = null!;
    AbilityResult _absentPathInput = null!;
    RecordedRequest _request = null!;
    RecordedRequest _escapedRequest = null!;

    async Task Establish()
    {
        await StartSystem();
        StartStage(Parsed(Block));
        _answer = _ => (200, """{"items":{"count":3}}""");
    }

    async Task Because()
    {
        _result = await _backend.Invoke(Call(input: """{"parcelId":"p 1","since":"2025-01-01T10:00:00Z"}"""), CancellationToken.None);
        _request = _received.Single();

        _absentPathInput = await _backend.Invoke(Call(input: "{}"), CancellationToken.None);

        _received.Clear();
        await _backend.Invoke(Call(input: """{"parcelId":"../../admin"}"""), CancellationToken.None);
        _escapedRequest = _received.Single();
    }

    [Fact] void should_escape_an_input_in_the_path_and_query() => _request.Target.ShouldEqual("/parcels/p%201/events?since=2025-01-01T10%3A00%3A00Z&kind=pickup&attempt=1");
    [Fact] void should_use_the_method_of_the_block() => _request.Method.ShouldEqual("GET");
    [Fact] void should_send_no_body() => _request.Body.ShouldEqual(string.Empty);
    [Fact] void should_return_the_status_and_a_nested_value() => ((AbilityReturned)_result).Output.ShouldEqual("""{"status":200,"count":3}""");
    [Fact] void should_not_send_when_a_path_input_is_absent() => ((AbilityNotSent)_absentPathInput).Reason.ShouldContain("parcelId");
    [Fact] void should_keep_a_traversal_inside_one_path_segment() => _escapedRequest.Target.StartsWith("/parcels/..%2F..%2Fadmin/events", StringComparison.Ordinal).ShouldBeTrue();
}
