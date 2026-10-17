// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_an_authored_clock : given.a_reaction_plan
{
    void Establish() => Compile(
        "reaction Worker\n  when Started\n    item\n    produces Followed\n      item = item",
        "then Started\n  for \"one\"\n  item = \"one\"\n  amount = 1\nthen Followed\n  for \"one\"\n  item = \"one\"",
        new(Action: "given clock \"2027-03-04T10:00:00.123Z\"\nwhen Begin\n  item = \"one\"\n  amount = 1"));

    async Task Because() => await Execute();

    [Fact] void should_settle_clocked_reactions_like_the_reference() => AssertMatchesReference();
}
