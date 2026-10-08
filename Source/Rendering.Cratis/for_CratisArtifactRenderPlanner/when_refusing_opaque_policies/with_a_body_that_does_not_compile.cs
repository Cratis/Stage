// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_refusing_opaque_policies;

public class with_a_body_that_does_not_compile : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = opaque_policy_model.Plan(opaque_policy_model.Source("return context.Occurred;"));

    [Fact] void should_render_nothing() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_name_the_refusal() => Assert.Contains(_plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-019" && diagnostic.Message.Contains("CS0029", StringComparison.Ordinal));
}
#endif
