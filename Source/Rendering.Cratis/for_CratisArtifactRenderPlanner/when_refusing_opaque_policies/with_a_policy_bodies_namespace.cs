// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_refusing_opaque_policies;

/// <summary>
/// An opaque policy's generated body class cannot share its name with a modeled namespace.
/// </summary>
public class with_a_policy_bodies_namespace : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = opaque_policy_model.Plan(opaque_policy_model.Source("return context.Identity.IsAuthenticated;")
        .Replace("module Billing", "module GeneratedPolicies", StringComparison.Ordinal)
        .Replace("feature Invoicing", "feature PolicyBodies", StringComparison.Ordinal));

    [Fact] void should_render_nothing() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_name_the_collision() => Assert.Contains(_plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Message.Contains("PolicyBodies", StringComparison.Ordinal));
}
#endif
