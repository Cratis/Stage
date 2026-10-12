// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class without_a_source_or_reaction : given.a_reaction_plan
{
    void Establish() => Compile("", "then no readmodel Marker for \"one\"", new() { Action = "when append Started\n  item = \"one\"\n  amount = 1" });
    Task Because() => Execute();
    [Fact] void should_append_and_compare_without_allocating_a_source() => AssertMatchesReference();
}
