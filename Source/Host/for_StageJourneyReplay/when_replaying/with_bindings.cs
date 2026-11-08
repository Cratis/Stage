// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Host.for_StageJourneyReplay.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageJourneyReplay.when_replaying;

public class with_bindings : a_journey_runner
{
    int _reads;

    private protected override TimeProvider Time { get; } = new fixed_time();

    protected override Task<HttpResponseMessage> Respond(HttpRequestMessage request, string body)
    {
        if (request.Method == HttpMethod.Post)
        {
            using var input = JsonDocument.Parse(body);
            var id = input.RootElement.GetProperty("projectId").GetString();
            return Task.FromResult(Answer(HttpStatusCode.OK, $$"""{"response":{"projectId":"{{id}}","count":2},"isSuccess":true}"""));
        }

        var rows = ++_reads == 1 ? "[]" : $$"""[{"projectId":"{{_requests[0].Body[14..50]}}","count":2}]""";
        return Task.FromResult(Answer(HttpStatusCode.OK, $$"""{"data":{{rows}},"isSuccess":true}"""));
    }

    Task Because() => Replay("""
        [
          { "name": "bound", "steps": [
            { "command": "RegisterProject", "input": { "projectId": "$new:project", "due": "${today+2d}", "at": "${now}", "label": "p-${project}" },
              "bind": { "count": "$.response.count" } },
            { "query": "LiveProjects", "arguments": { "projectId": "${project}", "since": "${today-1d}", "archived": false },
              "expect": { "result": [{ "projectId": "${project}", "count": "${count}" }] }, "bind": { "first": "$.result[0].projectId" } },
            { "query": "AllProjects", "arguments": { "projectId": "${missing}" } } ] }
        ]
        """);

    string Generated => Result(0, 0).Bindings!["project"].GetString()!;

    [Fact] void should_generate_a_guid() => Guid.TryParse(Generated, out _).ShouldBeTrue();
    [Fact] void should_send_the_generated_value() => _requests[0].Body.ShouldContain($"\"projectId\":\"{Generated}\"");
    [Fact] void should_interpolate_inside_a_string() => _requests[0].Body.ShouldContain($"\"label\":\"p-{Generated}\"");
    [Fact] void should_substitute_a_future_date() => _requests[0].Body.ShouldContain("\"due\":\"2026-03-03\"");
    [Fact] void should_substitute_now() => _requests[0].Body.ShouldContain("\"at\":\"2026-03-01T08:30:00.000Z\"");
    [Fact] void should_capture_a_response_value() => Result(0, 0).Bindings!["count"].GetInt32().ShouldEqual(2);
    [Fact] void should_send_the_query_arguments() => _requests[1].Uri.ShouldEqual($"/api/projects/live?waitForFirstResult=true&projectId={Generated}&since=2026-02-28&archived=false");
    [Fact] void should_match_a_typed_binding_after_re_reading() => Result(0, 1).Status.ShouldEqual(StageJourneyStepStatus.Passed);
    [Fact] void should_report_the_attempts() => Result(0, 1).Attempts.ShouldEqual(2);
    [Fact] void should_capture_a_result_value() => Result(0, 1).Bindings!["first"].GetString().ShouldEqual(Generated);
    [Fact] void should_error_an_unknown_binding() => Result(0, 2).Mismatches!.Single().Reason.ShouldEqual("unknown binding '${missing}'");

    sealed class fixed_time : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 3, 1, 8, 30, 0, TimeSpan.Zero);
    }
}
