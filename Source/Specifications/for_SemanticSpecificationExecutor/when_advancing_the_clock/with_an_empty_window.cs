// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_advancing_the_clock;

public class with_an_empty_window : when_settling_reactions.given.a_reaction_plan
{
    void Establish() => Compile(
        "reaction Worker\n  at 07:30 on Monday\n    produces Completed\n      for \"one\"",
        "then no events",
        new(Action: "given clock \"2026-10-05T07:30:00Z\"\nwhen clock \"2026-10-05T07:30:01Z\""));
    Task Because() => Execute();
    [Fact] void should_not_repeat_the_exclusive_start_like_the_reference() => AssertMatchesReference();
    [Fact] void should_append_nothing() => _result.Trace!.Facts.ShouldBeEmpty();
}
