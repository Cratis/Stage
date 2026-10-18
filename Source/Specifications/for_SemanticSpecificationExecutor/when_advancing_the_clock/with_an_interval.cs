// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_advancing_the_clock;

public class with_an_interval : when_settling_reactions.given.a_reaction_plan
{
    void Establish() => Compile(
        "reaction Worker\n  every 6 hours\n    produces Noted\n      for \"one\"\n      notedAt = $context.occurred",
        "then Noted\n  notedAt = \"2026-10-05T00:00:00Z\"\nthen Noted\n  notedAt = \"2026-10-05T06:00:00Z\"",
        new(Action: "given clock \"2026-10-04T23:00:00Z\"\nwhen clock \"2026-10-05T06:00:00Z\""));
    Task Because() => Execute();
    [Fact] void should_match_every_epoch_aligned_occurrence_in_the_reference() => AssertMatchesReference();
}
