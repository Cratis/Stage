// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_a_command_that_cascades : given.a_reaction_plan
{
    void Establish() => Compile("reaction Worker\n  when Started\n    item\n    produces Followed\n      item = item\nreaction Finisher\n  when Followed\n    item\n    invokes Complete\n      item = item", "then Started\n  item = \"one\"\n  amount = 1\nthen Followed\n  item = \"one\"\nthen Completed", action: "when Begin\n  item = \"one\"\n  amount = 1");
    Task Because() => Execute();
    [Fact] void should_match_the_reference_cascade() => AssertMatchesReference();
    [Fact] void should_append_all_three_facts() => _result.Trace!.Facts.Count.ShouldEqual(3);
}
