// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_a_rejected_destinationless_invocation : given.a_reaction_plan
{
    void Establish() => Compile("reaction Worker\n  when Started\n    invokes Complete\n      item = \"\"", "then error \"Item required\"", new() { Authorization = "validate\n  item not empty message \"Item required\"", CommandDestination = "" });
    Task Because() => Execute();
    [Fact] void should_validate_before_requiring_a_destination() => AssertMatchesReference();
}
