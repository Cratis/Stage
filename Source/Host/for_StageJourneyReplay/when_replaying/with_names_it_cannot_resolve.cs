// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_StageJourneyReplay.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageJourneyReplay.when_replaying;

public class with_names_it_cannot_resolve : a_journey_runner
{
    Task Because() => Replay("""
        [
          { "name": "ambiguous command", "steps": [{ "command": "Archive" }] },
          { "name": "ambiguous query", "steps": [{ "query": "Duplicate" }] },
          { "name": "unknown", "steps": [{ "command": "Nothing" }] },
          { "name": "qualified query", "steps": [{ "query": "Projects.AllProjects" }] }
        ]
        """);

    [Fact] void should_error_an_ambiguous_command() => Result(0, 0).Mismatches!.Single().Reason.ShouldEqual("command 'Archive' is ambiguous between Billing.Archive, Projects.Archive; use the qualified name");
    [Fact] void should_error_an_ambiguous_query() => Result(1, 0).Mismatches!.Single().Reason.ShouldEqual(Routes.Diagnostics.Single());
    [Fact] void should_error_an_unknown_name() => Result(2, 0).Mismatches!.Single().Reason.ShouldEqual("no modeled command is named 'Nothing'");
    [Fact] void should_report_errors_as_errors() => _report.Summary.StepsErrored.ShouldEqual(3);
    [Fact] void should_resolve_a_qualified_query_by_its_simple_name() => Result(3, 0).Route.ShouldEqual("/api/projects/all");
    [Fact] void should_send_only_the_resolved_step() => _requests.Count.ShouldEqual(1);
}
