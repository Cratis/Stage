// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_opaque_policy_specifications;

public class with_caller_predicates : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = opaque_policy_model.Plan(opaque_policy_specification_model.Source());

    [Fact] void should_admit_the_plan() => Assert.True(_plan.Success, opaque_policy_model.Errors(_plan));
    [Fact] void should_not_skip_specifications() => _plan.Refusals.ShouldBeEmpty();
    [Fact] void should_emit_command_and_query_scenarios() => _plan.Artifacts.Count(artifact => artifact.RelativePath.Contains("when_", StringComparison.Ordinal)).ShouldEqual(4);
}
#endif
