// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_generation_rules;

public class with_a_target_bound_body : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = opaque_policy_model.Plan(generation_rule_model.Load(generation_rule_model.TargetBody));

    [Fact] void should_bind_the_real_fundamentals_context() => Assert.True(_plan.Success, opaque_policy_model.Errors(_plan));
    [Fact] void should_compile_the_target_bound_body() => RenderedOutput.Errors(opaque_policy_model.Files(_plan)).ShouldBeEmpty();
}
#endif
