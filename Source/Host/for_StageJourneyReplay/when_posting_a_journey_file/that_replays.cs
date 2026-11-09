// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Host.for_StageJourneyReplay.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageJourneyReplay.when_posting_a_journey_file;

public class that_replays : a_replay_endpoint
{
    (int Status, string Body) _response;
    JsonElement Report => JsonDocument.Parse(_response.Body).RootElement;

    async Task Because() => _response = await Request(OneJourney, "?journey=register");

    [Fact] void should_answer_ok() => _response.Status.ShouldEqual(StatusCodes.Status200OK);
    [Fact] void should_report_the_schema() => Report.GetProperty("schema").GetString().ShouldEqual("stage-journey-report/v1");
    [Fact] void should_report_a_pass() => Report.GetProperty("passed").GetBoolean().ShouldBeTrue();
    [Fact] void should_report_camel_cased_steps() => Report.GetProperty("journeys")[0].GetProperty("steps")[0].GetProperty("status").GetString().ShouldEqual("passed");
    [Fact] void should_send_to_the_loopback_http_address() => _sent.Single().ShouldEqual(new Uri("http://127.0.0.1:9090/api/projects/register"));
}
