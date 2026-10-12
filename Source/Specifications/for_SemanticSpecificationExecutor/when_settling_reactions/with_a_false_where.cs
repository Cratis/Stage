// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_a_false_where : given.a_reaction_plan
{
    void Establish() => Compile("reaction Worker\n  when Started\n    item\n    amount\n    produces Followed\n      item = item\n  where amount > 1", "");
    Task Because() => Execute();
    [Fact] void should_match_the_reference() => AssertMatchesReference();
    [Fact] void should_append_only_the_action_fact() => _result.Trace!.Facts.Count.ShouldEqual(1);
}
