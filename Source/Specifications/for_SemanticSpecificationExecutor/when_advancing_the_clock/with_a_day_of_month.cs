// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_advancing_the_clock;

public class with_a_day_of_month : when_settling_reactions.given.a_reaction_plan
{
    void Establish() => Compile(
        "reaction Worker\n  at 06:00 on day 31\n    produces Noted\n      for \"one\"\n      notedAt = $context.occurred",
        "then Noted\n  notedAt = \"2026-01-31T06:00:00Z\"\nthen Noted\n  notedAt = \"2026-03-31T06:00:00Z\"",
        new(Action: "given clock \"2026-01-30T00:00:00Z\"\nwhen clock \"2026-03-31T06:00:00Z\""));
    Task Because() => Execute();
    [Fact] void should_skip_months_without_the_requested_day_like_the_reference() => AssertMatchesReference();
}
