// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageJourneyFile;

public class when_parsing_a_file_over_the_limits : Specification
{
    StageJourneyFileParse _parse = null!;

    void Because()
    {
        object deep = 1;
        for (var level = 0; level < 33; level++)
        {
            deep = new { next = deep };
        }

        var step = new { command = "A" };
        var journeys = Enumerable.Range(0, 101)
            .Select(index => (object)new { name = $"journey {index}", steps = Enumerable.Repeat(step, index == 0 ? 101 : 5).ToArray() })
            .Append(new { name = "deep", steps = new[] { new { command = "A", input = deep } } })
            .Append(new { name = new string('n', 201), steps = new[] { step } });
        _parse = StageJourneyFile.Parse(JsonSerializer.Serialize(new { schema = "stage-journey/v1", journeys }));
    }

    [Fact] void should_refuse_too_many_journeys() => _parse.Errors.ShouldContain("journeys: at most 100 journeys");
    [Fact] void should_refuse_too_many_steps_in_a_journey() => _parse.Errors.ShouldContain("journeys[0].steps: must be an array of 1 to 100 steps");
    [Fact] void should_refuse_too_many_steps_in_total() => _parse.Errors.ShouldContain("journeys: at most 500 steps in total");
    [Fact] void should_refuse_a_deep_input() => _parse.Errors.ShouldContain("journeys[101].steps[0].input: nested deeper than 32 levels");
    [Fact] void should_refuse_a_long_name() => _parse.Errors.ShouldContain("journeys[102].name: must be a string of 1 to 200 characters");
}
