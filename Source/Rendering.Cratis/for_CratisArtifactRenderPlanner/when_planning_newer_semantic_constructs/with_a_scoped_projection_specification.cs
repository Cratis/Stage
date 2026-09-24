// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_a_scoped_projection_specification : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = invoice_model.Plan(invoice_model.Compile(when_rendering_scoped_projections.ScopedSource));

    [Fact] void should_admit_scoped_projection_expectations() => _plan.Success.ShouldBeTrue();
    [Fact] void should_render_a_read_model_scenario() => Assert.True(_plan.Artifacts.Any(_ => _.RelativePath.EndsWith("when_registering_aproject_is_projected.cs", StringComparison.Ordinal)), string.Join(", ", _plan.Artifacts.Select(_ => _.RelativePath)));
    [Fact] void should_render_a_query_scenario() => _plan.Artifacts.Any(_ => _.RelativePath.EndsWith("when_registering_aproject_is_queried.cs", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_render_a_query_over_given_read_model_state() => _plan.Artifacts.Any(_ => _.RelativePath.EndsWith("when_looking_up_pinned_project_is_queried.cs", StringComparison.Ordinal)).ShouldBeTrue();
}
