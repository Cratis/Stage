// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_firing_startup_and_shutdown;

public class with_a_shutdown : when_settling_reactions.given.a_reaction_plan
{
    void Establish() => Compile("reaction Worker\n  when Shutdown\n    produces Completed\n      for \"one\"", "then Completed\n  for \"one\"", new(Action: "when trigger Shutdown"));
    Task Because() => Execute();
    [Fact] void should_match_the_reference() => AssertMatchesReference();
}
