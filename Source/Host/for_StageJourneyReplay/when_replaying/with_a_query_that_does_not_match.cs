// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Host.for_StageJourneyReplay.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageJourneyReplay.when_replaying;

public class with_a_query_that_does_not_match : a_journey_runner
{
    static readonly string _large = JsonSerializer.Serialize(new
    {
        data = Enumerable.Range(0, 2000).Select(index => new { name = $"Project {index:0000} with a long enough name to grow the body" }),
        isSuccess = true
    });

    protected override Task<HttpResponseMessage> Respond(HttpRequestMessage request, string body) =>
        Task.FromResult(Answer(HttpStatusCode.OK, request.RequestUri!.AbsolutePath == "/api/projects/live" ? _large : AllProjects));

    Task Because() => Replay("""
        [
          { "name": "wrong name", "steps": [{ "query": "AllProjects", "within": 250, "expect": { "result": [{ "name": "Apollo" }] } }] },
          { "name": "large", "steps": [{ "query": "LiveProjects", "within": 0, "expect": { "empty": true } }] },
          { "name": "absent", "steps": [{ "query": "AllProjects", "within": 0, "expect": { "absent": [{ "name": "Screenplay" }] } }] }
        ]
        """);

    [Fact] void should_fail_the_step() => Result(0, 0).Status.ShouldEqual(StageJourneyStepStatus.Failed);
    [Fact] void should_report_the_path() => Result(0, 0).Mismatches!.Single().Path.ShouldEqual("$.result[0]");
    [Fact] void should_report_the_expected_value() => Result(0, 0).Mismatches!.Single().Expected!.Value.GetProperty("name").GetString().ShouldEqual("Apollo");
    [Fact] void should_report_the_actual_result() => Result(0, 0).Actual!.Result!.Value[0].GetProperty("name").GetString().ShouldEqual("Screenplay");
    [Fact] void should_re_read_until_within_elapses() => Result(0, 0).Attempts!.Value.ShouldBeGreaterThan(1);
    [Fact] void should_read_once_when_within_is_zero() => Result(1, 0).Attempts.ShouldEqual(1);
    [Fact] void should_flag_a_truncated_result() => Result(1, 0).Actual!.Truncated.ShouldEqual(true);
    [Fact] void should_truncate_the_reported_result() => Result(1, 0).Actual!.Result!.Value.GetString()!.Length.ShouldEqual(StageJourneyRunner.ReportedBodyLimit);
    [Fact] void should_keep_the_live_marker_in_the_route() => _requests.Exists(request => request.Uri == "/api/projects/live?waitForFirstResult=true").ShouldBeTrue();
    [Fact] void should_fail_a_present_absent_value() => Result(2, 0).Mismatches!.Single().Path.ShouldEqual("$.absent[0]");
}
