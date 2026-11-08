// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Host.for_StageJourneyReplay.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageJourneyReplay.when_posting_a_journey_file;

public class that_is_refused : a_replay_endpoint
{
    readonly TaskCompletionSource<HttpResponseMessage> _held = new(TaskCreationOptions.RunContinuationsAsynchronously);
    (int Status, string Body) _invalid;
    (int Status, string Body) _unknownJourney;
    (int Status, string Body) _tooLarge;
    (int Status, string Body) _busy;
    (int Status, string Body) _first;
    (int Status, string Body) _loading;
    (int Status, string Body) _unsupported;
    (int Status, string Body) _plainText;

    protected override Task<HttpResponseMessage> Respond(HttpRequestMessage request) => _held.Task;

    async Task Because()
    {
        _plainText = await Request(OneJourney, contentType: "text/plain");
        _invalid = await Request("""{ "schema": "stage-journey/v2", "journeys": [] }""");
        _unknownJourney = await Request(OneJourney, "?journey=other");
        _tooLarge = await Request(new string(' ', StageJourneyFile.MaxBodyBytes + 1));
        var first = Request(OneJourney);
        for (var wait = 0; wait < 500 && _sent.Count == 0; wait++) await Task.Delay(10);
        _busy = await Request(OneJourney);
        _held.SetResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"isSuccess":true}""", Encoding.UTF8, "application/json") });
        _first = await first;
        _state = "loading";
        _loading = await Request(OneJourney);
        _state = "unsupported";
        _unsupported = await Request(OneJourney);
    }

    static JsonElement Problem((int Status, string Body) response) => JsonDocument.Parse(response.Body).RootElement;

    [Fact] void should_refuse_a_body_that_is_not_json() => _plainText.Status.ShouldEqual(StatusCodes.Status415UnsupportedMediaType);
    [Fact] void should_refuse_an_invalid_file() => _invalid.Status.ShouldEqual(StatusCodes.Status400BadRequest);
    [Fact] void should_list_every_problem() => Problem(_invalid).GetProperty("errors").EnumerateArray().Select(error => error.GetString()).ShouldContainOnly(["schema: must be 'stage-journey/v1'", "journeys: must be a non-empty array"]);
    [Fact] void should_name_the_capability() => Problem(_invalid).GetProperty("capability").GetString().ShouldEqual("Journey");
    [Fact] void should_refuse_an_unknown_journey() => Problem(_unknownJourney).GetProperty("errors")[0].GetString().ShouldEqual("journey: 'other' is not in the file");
    [Fact] void should_refuse_a_large_body() => _tooLarge.Status.ShouldEqual(StatusCodes.Status413PayloadTooLarge);
    [Fact] void should_refuse_a_second_replay_while_one_runs() => _busy.Status.ShouldEqual(StatusCodes.Status409Conflict);
    [Fact] void should_finish_the_running_replay() => _first.Status.ShouldEqual(StatusCodes.Status200OK);
    [Fact] void should_refuse_a_loading_session() => Problem(_loading).GetProperty("detail").GetString().ShouldEqual("The session is loading.");
    [Fact] void should_answer_service_unavailable() => _loading.Status.ShouldEqual(StatusCodes.Status503ServiceUnavailable);
    [Fact] void should_list_the_issues_of_an_unsupported_session() => Problem(_unsupported).GetProperty("errors")[0].GetString().ShouldEqual("World model: Chronicle registration was refused.");
}
