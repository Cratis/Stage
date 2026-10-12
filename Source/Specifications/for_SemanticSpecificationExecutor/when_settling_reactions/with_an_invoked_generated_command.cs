// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_an_invoked_generated_command : given.a_reaction_plan
{
    void Establish() => Compile("reaction Worker\n  when Started\n    item\n    invokes Complete\n      item = item", "", commandProperties: "token Token generated");
    Task Because() => Execute();
    [Fact] void should_match_the_reference_generation_refusal() => AssertMatchesReference();
    [Fact] void should_require_deterministic_fixtures() => _result.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.IdentityAllocation);
}
