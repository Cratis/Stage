// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_firing_application_triggers;

public class with_trigger_values : when_settling_reactions.given.a_reaction_plan
{
    void Establish() => Compile(
        "reaction Worker\n  when Arrived\n    item\n    produces Followed\n      for item\n      item = item",
        "then Followed\n  for \"one\"\n  item = \"one\"",
        new(Action: "when trigger Arrived\n  item = \"one\"", Policy: "trigger Arrived\n  item String"));
    Task Because() => Execute();
    [Fact] void should_match_the_reference() => AssertMatchesReference();
    [Fact] void should_use_the_trigger_identity_value() => _result.Trace!.Facts.Single().EventSource.ShouldEqual("\"one\"");
}
