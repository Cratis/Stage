// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_a_constrained_production : given.a_reaction_plan
{
    void Establish() => Compile("reaction Worker\n  when Started\n    item\n    produces Followed\n      item = item\n    produces Followed\n      item = item", "then error \"Already followed\"", declarations: "constraint Once\n  unique event Followed\n  message \"Already followed\"");
    Task Because() => Execute();
    [Fact] void should_reject_the_second_production_as_the_reference_does() => AssertMatchesReference();
    [Fact] void should_preserve_the_constraint_name() => _result.Trace!.RejectionCode.ShouldEqual("Once");
}
