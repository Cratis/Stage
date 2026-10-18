// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_advancing_the_clock;

public class with_a_weekly_schedule : when_settling_reactions.given.a_reaction_plan
{
    void Establish() => Compile(
        "reaction Worker\n  at 07:30 on Monday\n    produces Noted\n      for \"one\"\n      notedAt = $context.occurred",
        "then Noted\n  notedAt = \"2026-10-05T07:30:00Z\"",
        new(Action: "given clock \"2026-10-04T23:00:00Z\"\nwhen clock \"2026-10-06T08:00:00Z\""));
    Task Because() => Execute();
    [Fact] void should_match_the_utc_schedule_in_the_reference() => AssertMatchesReference();
}
