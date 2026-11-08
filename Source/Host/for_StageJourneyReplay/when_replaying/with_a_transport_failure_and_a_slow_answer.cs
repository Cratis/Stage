// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Cratis.Stage.Host.for_StageJourneyReplay.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageJourneyReplay.when_replaying;

public class with_a_transport_failure_and_a_slow_answer : a_journey_runner
{
    protected override TimeSpan StepTimeout => TimeSpan.FromMilliseconds(100);

    protected override async Task<HttpResponseMessage> Respond(HttpRequestMessage request, string body)
    {
        if (request.RequestUri!.AbsolutePath == "/api/projects/register") throw new HttpRequestException("Connection refused");
        await Task.Delay(TimeSpan.FromSeconds(10), Time);

        return Answer(HttpStatusCode.OK, Accepted);
    }

    Task Because() => Replay("""
        [
          { "name": "refused", "steps": [{ "command": "RegisterProject" }] },
          { "name": "slow", "steps": [{ "command": "Projects.ApplyLateFee" }] }
        ]
        """);

    [Fact] void should_error_the_refused_step() => Result(0, 0).Status.ShouldEqual(StageJourneyStepStatus.Error);
    [Fact] void should_report_the_transport_failure() => Result(0, 0).Mismatches!.Single().Reason.ShouldEqual("the request failed: Connection refused");
    [Fact] void should_report_the_step_timeout() => Result(1, 0).Mismatches!.Single().Reason.ShouldEqual("the step timed out after 0 seconds");
}
