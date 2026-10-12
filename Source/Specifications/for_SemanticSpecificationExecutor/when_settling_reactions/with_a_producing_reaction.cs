// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_a_producing_reaction : given.a_reaction_plan
{
    void Establish() => Compile("reaction Worker\n  when Started\n    item\n    produces Followed\n      item = item", "then Followed\n  for \"one\"\n  item = \"one\"");
    Task Because() => Execute();
    [Fact] void should_match_the_reference() => AssertMatchesReference();
    [Fact] void should_preserve_the_trigger_destination() => _result.Trace!.Facts[^1].EventSource.ShouldEqual("\"one\"");
    [Fact] void should_leave_the_production_unrouted() => _result.Trace!.Facts[^1].Route.ShouldBeNull();
}
