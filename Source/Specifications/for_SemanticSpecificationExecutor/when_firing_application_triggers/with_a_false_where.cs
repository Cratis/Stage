// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_firing_application_triggers;

public class with_a_false_where : when_settling_reactions.given.a_reaction_plan
{
    void Establish() => Compile(
        "reaction Worker\n  when Arrived\n    item\n    amount\n    produces Followed\n      for item\n      item = item\n  where amount > 1",
        "then no events",
        new(Action: "when trigger Arrived\n  item = \"one\"\n  amount = 1", Policy: "trigger Arrived\n  item String\n  amount Decimal"));
    Task Because() => Execute();
    [Fact] void should_match_the_reference() => AssertMatchesReference();
    [Fact] void should_append_nothing() => _result.Trace!.Facts.ShouldBeEmpty();
}
