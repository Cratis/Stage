// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_an_invoking_reaction : given.a_reaction_plan
{
    void Establish() => Compile("reaction Worker\n  when Started\n    item\n    invokes Complete\n      item = item", "then Completed\n  for \"one\"");
    Task Because() => Execute();
    [Fact] void should_match_the_reference_pipeline_outcome() => AssertMatchesReference();
    [Fact] void should_append_the_invoked_command_fact() => _result.Trace!.Facts.Count.ShouldEqual(2);
}
