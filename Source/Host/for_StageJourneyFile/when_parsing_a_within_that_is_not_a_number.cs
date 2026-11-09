// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageJourneyFile;

public class when_parsing_a_within_that_is_not_a_number : Specification
{
    [Theory]
    [InlineData("\"1000\"")]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("{ }")]
    [InlineData("[1]")]
    [InlineData("1.5")]
    public void should_refuse_it_without_throwing(string within)
    {
        var parse = StageJourneyFile.Parse($$"""
            { "schema": "stage-journey/v1", "journeys": [ { "name": "j", "steps": [ { "query": "B", "within": {{within}} } ] } ] }
            """);

        parse.File.ShouldBeNull();
        parse.Errors.ShouldContain("journeys[0].steps[0].within: must be a whole number of milliseconds");
    }
}
