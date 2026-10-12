// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_constrained_unsourced_facts : given.a_reaction_plan
{
    void Establish() => Compile("reaction Worker\n  when Started\n    item\n    produces Followed\n      item = item\n    produces Followed\n      item = item", "then error \"Already followed\"", new()
    {
        Action = "when append Started\n  item = \"one\"\n  amount = 1",
        Declarations = "constraint Once\n  unique event Followed\n  message \"Already followed\""
    });
    Task Because() => Execute();
    [Fact] void should_enforce_constraints_at_the_null_destination() => AssertMatchesReference();
    [Fact] void should_preserve_the_constraint_name() => _result.Trace!.RejectionCode.ShouldEqual("Once");
}
