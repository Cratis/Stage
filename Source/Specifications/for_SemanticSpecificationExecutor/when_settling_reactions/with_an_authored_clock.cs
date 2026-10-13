// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_an_authored_clock : given.a_reaction_plan
{
    void Establish() => Compile(
        "reaction Worker\n  when Started\n    item\n    produces Followed\n      item = item",
        "then Followed\n  for \"one\"\n  item = \"one\"",
        new(Action: "given clock \"2027-03-04T10:00:00.123Z\"\nwhen Begin\n  item = \"one\"\n  amount = 1"));

    async Task Because() => await Execute();

    [Fact] void should_keep_reaction_clocks_refused() => _result.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Time);
    [Fact] void should_not_report_a_clocked_reaction_as_passing() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
}
