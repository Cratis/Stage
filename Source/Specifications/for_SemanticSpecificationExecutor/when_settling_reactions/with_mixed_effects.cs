// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_mixed_effects : given.a_reaction_plan
{
    void Establish() => Compile("reaction Worker\n  when Started\n    item\n    produces Followed\n      item = item\n    invokes Complete\n      item = item", "then Followed\n  item = \"one\"\nthen Completed");
    Task Because() => Execute();
    [Fact] void should_match_the_reference_mixed_effects() => AssertMatchesReference();
    [Fact] void should_produce_before_invoking() => _result.Trace!.Facts.Count.ShouldEqual(3);
}
