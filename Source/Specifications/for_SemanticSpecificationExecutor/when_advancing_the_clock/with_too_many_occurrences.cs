// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_advancing_the_clock;

public class with_too_many_occurrences : when_settling_reactions.given.a_reaction_plan
{
    void Establish() => Compile(
        "reaction Worker\n  every 1 second\n    produces Completed\n      for \"one\"",
        "then no events",
        new(Action: "given clock \"1970-01-01T00:00:00Z\"\nwhen clock \"1970-01-01T02:46:41Z\""));
    Task Because() => Execute();
    [Fact] void should_refuse_like_the_reference() => AssertMatchesReference();
    [Fact] void should_report_the_shared_occurrence_limit() => _result.Unsupported!.Details.ShouldEqual("Advancing the clock makes more than 10000 occurrences due.");
}
