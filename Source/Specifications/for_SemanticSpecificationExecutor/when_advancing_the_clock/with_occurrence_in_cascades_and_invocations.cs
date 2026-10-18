// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_advancing_the_clock;

public class with_occurrence_in_cascades_and_invocations : when_settling_reactions.given.a_reaction_plan
{
    void Establish() => Compile(
        """
        reaction Clock
          every 1 hour
            produces Completed
              for "one"
        reaction Cascade
          when Completed
            produces Noted
              notedAt = $context.occurred
            invokes Stamp
              item = "one"
              time = $context.occurred
        """,
        """
        then Completed
        then Noted
          notedAt = "2026-10-05T01:00:00Z"
        then Stamped
          at = "2026-10-05T01:00:00Z"
        then Completed
        then Noted
          notedAt = "2026-10-05T02:00:00Z"
        then Stamped
          at = "2026-10-05T02:00:00Z"
        """,
        new(Action: "given clock \"2026-10-05T00:30:00Z\"\nwhen clock \"2026-10-05T02:00:00Z\"", Declarations: """
            event Stamped
              at DateTime
            command Stamp
              item String identifier
              time DateTime
              produces Stamped
                for item
                at = $context.occurred
            """));
    Task Because() => Execute();
    [Fact] void should_keep_each_occurrence_through_cascades_and_command_clocks() => AssertMatchesReference();
}
