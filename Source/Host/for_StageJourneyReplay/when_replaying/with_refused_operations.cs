// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Cratis.Stage.Host.for_StageJourneyReplay.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageJourneyReplay.when_replaying;

public class with_refused_operations : a_journey_runner
{
    protected override Task<HttpResponseMessage> Respond(HttpRequestMessage request, string body) => Task.FromResult(request.RequestUri!.AbsolutePath switch
    {
        "/api/projects/register" => Answer(HttpStatusCode.Forbidden, Forbidden),
        "/api/projects/apply-late-fee" => Answer(HttpStatusCode.NotImplemented, Unsupported, "ImplementationBody"),
        _ => Answer(HttpStatusCode.ServiceUnavailable, string.Empty)
    });

    Task Because() => Replay("""
        [
          { "name": "refused", "steps": [
            { "command": "RegisterProject", "expect": { "outcome": "unauthorized", "messages": ["not authorized"] } },
            { "command": "Projects.ApplyLateFee", "expect": { "outcome": "unsupported", "messages": ["implementation bodies"] } } ] },
          { "name": "not ready", "steps": [{ "query": "AllProjects" }] }
        ]
        """);

    [Fact] void should_pass_an_expected_unauthorized_step() => Result(0, 0).Status.ShouldEqual(StageJourneyStepStatus.Passed);
    [Fact] void should_pass_an_expected_unsupported_step() => Result(0, 1).Status.ShouldEqual(StageJourneyStepStatus.Passed);
    [Fact] void should_error_an_answer_that_is_not_json() => Result(1, 0).Mismatches!.Single().Reason.ShouldEqual("the answer is not a JSON object (HTTP 503)");
}
