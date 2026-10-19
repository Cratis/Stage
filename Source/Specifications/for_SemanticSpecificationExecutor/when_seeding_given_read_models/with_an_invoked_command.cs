// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_seeding_given_read_models;

public class with_an_invoked_command : a_reaction_plan
{
    void Establish() => Compile(
        "reaction Worker\n  when Started\n    item\n    invokes Complete\n      item = item",
        "then Completed\n  for \"one\"",
        new ReactionScenario(
            Action: """
                given Recorded
                  for "one"
                  name = "history"
                given readmodel Details
                  id = "one"
                  name = "seed"
                when append Started
                  for "one"
                  item = "one"
                  amount = 1
                """,
            Policy: "command reads protected",
            CommandProperties: "reads Details by item\nvalidate\n  require Details.name == \"seed\"\n    message \"Unexpected name.\"",
            Declarations: """
                command Record
                  item String identifier
                  name String
                  produces Recorded
                    for item
                    name = name
                event Recorded
                  name String
                event Erased
                readmodel Details
                  id String key
                  name String
                projection RecordDetails => Details
                  from Recorded
                    id = $eventSourceId
                    name = name
                  remove with Erased
                """));

    Task Because() => Execute();

    [Fact] void should_share_the_root_seed_with_the_invocation() => AssertMatchesReference();
    [Fact] void should_append_the_invoked_command_fact() => _result.Trace!.Facts.Count.ShouldEqual(2);
}
