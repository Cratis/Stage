// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_StageJourneyReplay.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageJourneyReplay.when_posting_a_journey_file;

public class that_has_a_within_of_the_wrong_kind : a_replay_endpoint
{
    (int Status, string Body) _text;
    (int Status, string Body) _nothing;
    (int Status, string Body) _boolean;
    (int Status, string Body) _object;

    async Task Because()
    {
        _text = await Request(Journey("\"1000\""));
        _nothing = await Request(Journey("null"));
        _boolean = await Request(Journey("true"));
        _object = await Request(Journey("{ }"));
    }

    static string Journey(string within) => $$"""{ "schema": "stage-journey/v1", "journeys": [{ "name": "j", "steps": [{ "query": "B", "within": {{within}} }] }] }""";

    [Fact] void should_refuse_text_as_a_bad_request() => _text.Status.ShouldEqual(StatusCodes.Status400BadRequest);
    [Fact] void should_refuse_null_as_a_bad_request() => _nothing.Status.ShouldEqual(StatusCodes.Status400BadRequest);
    [Fact] void should_refuse_a_boolean_as_a_bad_request() => _boolean.Status.ShouldEqual(StatusCodes.Status400BadRequest);
    [Fact] void should_refuse_an_object_as_a_bad_request() => _object.Status.ShouldEqual(StatusCodes.Status400BadRequest);
    [Fact] void should_name_the_problem() => _text.Body.ShouldContain("journeys[0].steps[0].within: must be a whole number of milliseconds");
}
