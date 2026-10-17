// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_an_offset_occurrence : given.a_reaction_plan
{
    void Establish() => Compile(
        "reaction Worker\n  when Started\n    produces Noted\n      notedAt = $context.occurred",
        "then Started\n  for \"one\"\n  item = \"one\"\n  amount = 1\nthen Noted\n  for \"one\"\n  notedAt = \"2027-03-04T08:00:00.1230000Z\"",
        new(Action: "given clock \"2027-03-04T10:00:00.123+02:00\"\nwhen Begin\n  item = \"one\"\n  amount = 1"));

    Task Because() => Execute();

    [Fact] void should_match_the_reference_occurrence() => AssertMatchesReference();
    [Fact] void should_record_exact_utc_round_trip_text() => _result.Trace!.Facts[^1].Values.Single().Value.ShouldEqual("\"2027-03-04T08:00:00.1230000Z\"");
}
