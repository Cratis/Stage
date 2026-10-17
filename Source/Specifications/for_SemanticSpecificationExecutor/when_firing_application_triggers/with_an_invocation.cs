// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_firing_application_triggers;

public class with_an_invocation : when_settling_reactions.given.a_reaction_plan
{
    void Establish() => Compile(
        "reaction Worker\n  when Arrived\n    item\n    invokes Complete\n      item = item",
        "then Completed\n  for \"one\"",
        new(Action: "when trigger Arrived\n  item = \"one\"", Policy: "trigger Arrived\n  item String"));
    Task Because() => Execute();
    [Fact] void should_run_the_invoked_command_like_the_reference() => AssertMatchesReference();
}
