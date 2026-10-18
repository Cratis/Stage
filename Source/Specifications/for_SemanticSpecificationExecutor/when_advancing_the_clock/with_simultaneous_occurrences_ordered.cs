// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_advancing_the_clock;

public class with_simultaneous_occurrences_ordered : when_settling_reactions.given.a_reaction_plan
{
    void Establish() => Compile(
        "reaction Worker\n  every 1 hour\n    produces Completed\n      for \"interval\"\n  at 01:00\n    produces Followed\n      for \"schedule\"\n      item = \"schedule\"",
        "then Completed\n  for \"interval\"\nthen Followed\n  for \"schedule\"\n  item = \"schedule\"",
        new(Action: "given clock \"2026-10-05T00:30:00Z\"\nwhen clock \"2026-10-05T01:00:00Z\""));
    Task Because() => Execute();
    [Fact] void should_preserve_trigger_declaration_order_like_the_reference() => AssertMatchesReference();
}
