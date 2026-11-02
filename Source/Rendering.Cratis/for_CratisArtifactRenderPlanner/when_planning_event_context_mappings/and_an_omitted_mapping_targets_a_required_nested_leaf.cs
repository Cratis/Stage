// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_event_context_mappings;

public class and_an_omitted_mapping_targets_a_required_nested_leaf : Specification
{
    ArtifactRenderPlan _plan = null!;
    ArtifactRenderPlan _strict = null!;

    void Because()
    {
        _plan = event_context_projection_model.Plan(event_context_projection_model.RequiredNestedLeaf);
        _strict = event_context_projection_model.Plan(event_context_projection_model.RequiredNestedLeaf, strict: true);
    }

    [Fact] void should_render_the_projection() => _plan.Success.ShouldBeTrue();
    [Fact] void should_render_the_nested_mapping() => event_context_projection_model.Source(_plan).ShouldContain("Set(model => model.Details.Seen).ToEventContextProperty(c => c.Occurred)");
    [Fact] void should_not_produce_a_degradation() => _plan.Degradations.IsEmpty.ShouldBeTrue();
    [Fact] void should_succeed_when_strict() => _strict.Success.ShouldBeTrue();
    [Fact] void should_compile_the_generated_backend() => event_context_projection_model.AssertCompiles(_plan);
}
