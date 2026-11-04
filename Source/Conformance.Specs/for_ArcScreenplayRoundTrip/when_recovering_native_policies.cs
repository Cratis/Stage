// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip;

public class when_recovering_native_policies : Specification
{
    [Theory]
    [InlineData("BookEntry", "Accountant", true)]
    [InlineData("RunJob", "Automation", false)]
    public void should_recover_the_exact_policy_without_adding_authentication(string command, string role, bool authenticated)
    {
        var recovered = new given.arc_recovery(given.native_policy_sources.All());
        recovered.AssertHealthy();
        AssertPolicy(recovered.Model, command, role, authenticated);
        Assert.DoesNotContain("SP0026", recovered.Result.Diagnostics.Select(diagnostic => diagnostic.Code));
    }

    internal static void AssertPolicy(ExecutableSemanticModel model, string command, string role, bool authenticated)
    {
        var operation = given.arc_recovery.Commands(model).Single(candidate => candidate.Name == command);
        var reference = Assert.IsType<SemanticPolicyReference>(operation.Authorization);
        var policy = model.Application.Policies.Single(candidate => candidate.Name == reference.Name);
        var condition = policy.Condition;
        if (authenticated)
        {
            var logical = Assert.IsType<SemanticLogicalPolicyCondition>(condition);
            Assert.Equal(SemanticLogicalOperator.And, logical.Operator);
            Assert.IsType<SemanticAuthenticatedCondition>(logical.Left);
            condition = logical.Right;
        }
        Assert.Equal(role, Assert.IsType<SemanticRoleCondition>(condition).Role);
    }
}
#endif
