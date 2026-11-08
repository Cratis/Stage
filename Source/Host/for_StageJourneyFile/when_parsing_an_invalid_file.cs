// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageJourneyFile;

public class when_parsing_an_invalid_file : Specification
{
    const string Text = """
        {
          "schema": "stage-journey/v2",
          "extra": true,
          "journeys": [
            {
              "name": "first",
              "steps": [
                { "command": "A", "query": "B" },
                { },
                { "command": "A", "persona": "admin" },
                { "command": "A", "expect": { "outcome": "maybe" } },
                { "command": "A", "expect": { "messages": ["x"] } },
                { "command": "A", "expect": { "outcome": "rejected", "response": { } } },
                { "query": "B", "arguments": { "nested": { "a": 1 } }, "input": { } },
                { "command": "A", "expect": { "result": [], "list": "sorted" }, "within": 10 },
                { "query": "B", "bind": { "id": "$.response.id" }, "note": 5 }
              ]
            },
            { "name": "first", "steps": [] }
          ]
        }
        """;

    StageJourneyFileParse _parse = null!;

    void Because() => _parse = StageJourneyFile.Parse(Text);

    [Fact] void should_not_produce_a_file() => _parse.File.ShouldBeNull();
    [Fact] void should_refuse_another_schema() => _parse.Errors.ShouldContain("schema: must be 'stage-journey/v1'");
    [Fact] void should_refuse_an_unknown_property() => _parse.Errors.ShouldContain("extra: unknown property");
    [Fact] void should_refuse_both_command_and_query() => _parse.Errors.ShouldContain("journeys[0].steps[0]: exactly one of command or query");
    [Fact] void should_refuse_neither_command_nor_query() => _parse.Errors.ShouldContain("journeys[0].steps[1]: exactly one of command or query");
    [Fact] void should_refuse_a_persona() => _parse.Errors.ShouldContain("journeys[0].steps[2].persona: not supported: the sandbox plays the persona each operation requires");
    [Fact] void should_refuse_an_unknown_outcome() => _parse.Errors.ShouldContain("journeys[0].steps[3].expect.outcome: must be one of accepted, rejected, unauthorized, unsupported");
    [Fact] void should_refuse_messages_when_accepted() => _parse.Errors.ShouldContain("journeys[0].steps[4].expect.messages: only with a rejected, unauthorized or unsupported outcome");
    [Fact] void should_refuse_a_response_when_rejected() => _parse.Errors.ShouldContain("journeys[0].steps[5].expect.response: only with an accepted outcome");
    [Fact] void should_refuse_a_nested_argument() => _parse.Errors.ShouldContain("journeys[0].steps[6].arguments.nested: must be a string, number or boolean");
    [Fact] void should_refuse_input_on_a_query() => _parse.Errors.ShouldContain("journeys[0].steps[6].input: only for a command step");
    [Fact] void should_refuse_a_result_on_a_command() => _parse.Errors.ShouldContain("journeys[0].steps[7].expect.result: not for this kind of step");
    [Fact] void should_refuse_an_unknown_list_match() => _parse.Errors.ShouldContain("journeys[0].steps[7].expect.list: must be one of contains, ordered, exact");
    [Fact] void should_refuse_within_on_a_command() => _parse.Errors.ShouldContain("journeys[0].steps[7].within: only for a query step");
    [Fact] void should_refuse_a_bind_path_of_the_other_kind() => _parse.Errors.ShouldContain("journeys[0].steps[8].bind.id: must be a path under $.result of .name and [index] segments");
    [Fact] void should_refuse_a_note_that_is_not_text() => _parse.Errors.ShouldContain("journeys[0].steps[8].note: must be a string of at most 1000 characters");
    [Fact] void should_refuse_a_duplicate_journey_name() => _parse.Errors.ShouldContain("journeys[1].name: 'first' is used by an earlier journey");
    [Fact] void should_refuse_a_journey_without_steps() => _parse.Errors.ShouldContain("journeys[1].steps: must be an array of 1 to 100 steps");
}
