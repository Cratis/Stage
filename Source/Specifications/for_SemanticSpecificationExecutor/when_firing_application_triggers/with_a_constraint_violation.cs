// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_firing_application_triggers;

public class with_a_constraint_violation : when_settling_reactions.given.a_reaction_plan
{
    void Establish() => Compile(
        "reaction Worker\n  when Arrived\n    item\n    produces Followed\n      for item\n      item = item\n    produces Followed\n      for item\n      item = item",
        "then error \"Already followed\"",
        new(Action: "when trigger Arrived\n  item = \"one\"", Policy: "trigger Arrived\n  item String", Declarations: "constraint Once\n  unique event Followed\n  message \"Already followed\""));
    Task Because() => Execute();
    [Fact] void should_reject_like_the_reference() => AssertMatchesReference();
    [Fact] void should_preserve_the_constraint_name() => _result.Trace!.RejectionCode.ShouldEqual("Once");
}
