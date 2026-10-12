// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_a_system_identity : given.a_reaction_plan
{
    [Theory]
    [InlineData("authenticated", "runs as system", true)]
    [InlineData("role \"Automation\"", "runs as system role \"Automation\"", true)]
    [InlineData("role \"Automation\"", "runs as system", false)]
    [InlineData("claim \"actor\" matches \"system\"", "runs as system", false)]
    [InlineData("not claim \"actor\" matches \"system\"", "runs as system", false)]
    [InlineData("role \"Automation\" or claim \"actor\" matches \"system\"", "runs as system role \"Automation\"", true)]
    public async Task should_use_only_the_declared_roles_and_keep_claims_unknown(string condition, string identity, bool accepted)
    {
        Compile($"reaction Worker\n  {identity}\n  when Started\n    item\n    invokes Complete\n      item = item", accepted ? "then Completed" : "then denied", policy: $"policy Access\n  require {condition}", authorization: "authorize Access");
        await Execute();
        AssertMatchesReference();
    }
}
