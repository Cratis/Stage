// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_an_unsourced_append : given.a_reaction_plan
{
    void Establish() => Compile("reaction Worker\n  when Started\n    item\n    invokes Complete\n      item = item", "then readmodel Marker\n  item = \"one\"", new() { Action = "when append Started\n  item = \"one\"\n  amount = 1" });
    Task Because() => Execute();
    [Fact] void should_ignore_the_unobserved_null_destination_in_replay() => AssertMatchesReference();
    [Fact] void should_materialize_the_invoked_explicit_destination() => _result.Trace!.ReadModels.Count.ShouldEqual(1);
}
