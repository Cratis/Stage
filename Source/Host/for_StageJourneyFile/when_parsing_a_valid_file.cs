// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageJourneyFile;

public class when_parsing_a_valid_file : Specification
{
    const string Text = """
        {
          "schema": "stage-journey/v1",
          "journeys": [
            {
              "name": "register then list",
              "note": "recorded from the original system",
              "evidence": ["har:entries[3]"],
              "steps": [
                { "name": "register", "command": "Projects.RegisterProject", "input": { "projectId": "$new:project", "name": "Apollo" },
                  "bind": { "project": "$.response.projectId" } },
                { "command": "RegisterProject", "input": { "name": "" }, "expect": { "outcome": "rejected", "messages": ["required"] } },
                { "query": "AllProjects", "arguments": { "page": 1 }, "within": 90000,
                  "expect": { "result": [{ "name": "Apollo" }], "absent": [{ "name": "" }], "list": "exact" } },
                { "query": "ArchivedProjects", "expect": { "empty": true } }
              ]
            }
          ]
        }
        """;

    StageJourneyFileParse _parse = null!;
    StageJourney Journey => _parse.File!.Journeys.Single();

    void Because() => _parse = StageJourneyFile.Parse(Text);

    [Fact] void should_have_no_errors() => _parse.Errors.ShouldBeEmpty();
    [Fact] void should_read_the_journey_name() => Journey.Name.ShouldEqual("register then list");
    [Fact] void should_read_every_step() => Journey.Steps.Count.ShouldEqual(4);
    [Fact] void should_read_a_command_step() => Journey.Steps[0].Kind.ShouldEqual(StageJourneyStepKind.Command);
    [Fact] void should_read_a_query_step() => Journey.Steps[2].Kind.ShouldEqual(StageJourneyStepKind.Query);
    [Fact] void should_keep_the_target_as_written() => Journey.Steps[0].Target.ShouldEqual("Projects.RegisterProject");
    [Fact] void should_default_to_an_accepted_outcome() => Journey.Steps[0].Expect.Outcome.ShouldEqual(StageJourneyOutcome.Accepted);
    [Fact] void should_read_the_rejected_messages() => Journey.Steps[1].Expect.Messages.ShouldContainOnly(["required"]);
    [Fact] void should_read_the_list_match() => Journey.Steps[2].Expect.List.ShouldEqual(StageJourneyListMatch.Exact);
    [Fact] void should_read_the_absent_values() => Journey.Steps[2].Expect.Absent.Count.ShouldEqual(1);
    [Fact] void should_clamp_within() => Journey.Steps[2].Within.ShouldEqual(TimeSpan.FromSeconds(30));
    [Fact] void should_default_within() => Journey.Steps[3].Within.ShouldEqual(TimeSpan.FromSeconds(5));
    [Fact] void should_read_empty() => Journey.Steps[3].Expect.Empty.ShouldBeTrue();
    [Fact] void should_read_the_bindings() => Journey.Steps[0].Bind["project"].ShouldEqual("$.response.projectId");
    [Fact] void should_read_the_note() => Journey.Note.ShouldEqual("recorded from the original system");
    [Fact] void should_read_the_evidence() => Journey.Evidence.ShouldContainOnly(["har:entries[3]"]);
}
