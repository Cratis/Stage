// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_opaque_policies;

/// <summary>
/// An unused opaque policy does not reserve the PolicyBodies type in a portable-only render.
/// </summary>
public class without_an_opaque_policy_use_site : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = opaque_policy_model.Plan(opaque_policy_model.Source("return context.Identity.IsAuthenticated;", "Staff", "Staff")
        .Replace("module Billing", "module GeneratedPolicies", StringComparison.Ordinal)
        .Replace("feature Invoicing", "feature PolicyBodies", StringComparison.Ordinal));

    [Fact] void should_admit_the_plan() => Assert.True(_plan.Success, opaque_policy_model.Errors(_plan));
    [Fact] void should_not_emit_opaque_bodies() => Assert.DoesNotContain(_plan.Artifacts, artifact => artifact.RelativePath == "GeneratedPolicies/PolicyBodies.cs");
}
#endif
