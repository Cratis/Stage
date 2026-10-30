// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies.for_SemanticPolicyNames;

public class when_two_operations_use_one_opaque_policy : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = opaque_policy_model.Plan(opaque_policy_model.Source("return context.Identity.IsAuthenticated;"));

    [Fact] void should_plan_the_application() => _plan.Success.ShouldBeTrue();
    [Fact] void should_qualify_both() => _plan.Artifacts.Where(artifact => artifact.RelativePath.StartsWith("GeneratedPolicies/", StringComparison.Ordinal) && artifact.RelativePath.EndsWith("Policy.cs", StringComparison.Ordinal))
        .Select(artifact => artifact.RelativePath).Order().ShouldEqual(["GeneratedPolicies/CustomForInvoiceByIdPolicy.cs", "GeneratedPolicies/CustomForIssueInvoicePolicy.cs"]);
}
#endif
