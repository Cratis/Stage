// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_a_denied_destinationless_invocation : given.a_reaction_plan
{
    void Establish() => Compile("reaction Worker\n  when Started\n    item\n    invokes Complete\n      item = item", "then denied", new() { Policy = "policy Access\n  require role \"Automation\"", Authorization = "authorize Access", CommandDestination = "" });
    Task Because() => Execute();
    [Fact] void should_deny_before_requiring_a_destination() => AssertMatchesReference();
}
