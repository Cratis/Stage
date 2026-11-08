// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Cratis.Stage.Host.for_StageJourneyReplay.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageJourneyReplay.when_replaying;

public class with_a_failing_step : a_journey_runner
{
    int _registrations;

    protected override Task<HttpResponseMessage> Respond(HttpRequestMessage request, string body) => Task.FromResult(request.RequestUri!.AbsolutePath switch
    {
        "/api/projects/register" => ++_registrations == 1 ? Answer(HttpStatusCode.OK, Accepted) : Answer(HttpStatusCode.BadRequest, Rejected),
        _ => Answer(HttpStatusCode.OK, AllProjects)
    });

    Task Because() => Replay("""
        [
          { "name": "register twice", "steps": [
            { "name": "register", "command": "Projects.RegisterProject", "input": { "name": "Screenplay" }, "expect": { "response": { "name": "Screenplay" } } },
            { "name": "again", "command": "RegisterProject", "input": { "name": "Screenplay" } },
            { "query": "AllProjects" } ] },
          { "name": "list", "steps": [
            { "query": "AllProjects", "expect": { "result": [{ "name": "Screenplay" }] } } ] }
        ]
        """);

    [Fact] void should_fail_the_run() => _report.Passed.ShouldBeFalse();
    [Fact] void should_report_the_live_isolation() => _report.Isolation.ShouldEqual("live");
    [Fact] void should_pass_the_first_step() => Result(0, 0).Status.ShouldEqual(StageJourneyStepStatus.Passed);
    [Fact] void should_fail_the_second_step() => Result(0, 1).Status.ShouldEqual(StageJourneyStepStatus.Failed);
    [Fact] void should_report_the_outcome_mismatch() => Result(0, 1).Mismatches!.Single().Reason.ShouldEqual("expected accepted, found rejected");
    [Fact] void should_report_the_actual_messages() => Result(0, 1).Actual!.Messages.ShouldContainOnly(["Project name is already in use"]);
    [Fact] void should_report_the_http_status() => Result(0, 1).HttpStatus.ShouldEqual(400);
    [Fact] void should_skip_the_rest_of_the_journey() => Result(0, 2).Status.ShouldEqual(StageJourneyStepStatus.Skipped);
    [Fact] void should_still_pass_the_other_journey() => _report.Journeys[1].Passed.ShouldBeTrue();
    [Fact] void should_not_send_the_skipped_step() => _requests.Count.ShouldEqual(3);
    [Fact] void should_post_the_input() => _requests[0].Body.ShouldEqual("""{"name":"Screenplay"}""");
    [Fact] void should_count_the_steps() => _report.Summary.ShouldEqual(new StageJourneySummary(2, 1, 4, 2, 1, 1, 0));
}
