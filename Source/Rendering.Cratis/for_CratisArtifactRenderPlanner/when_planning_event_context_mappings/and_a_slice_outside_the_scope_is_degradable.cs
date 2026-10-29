// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_event_context_mappings;

public class and_a_slice_outside_the_scope_is_degradable : Specification
{
    ArtifactRenderPlan _plan = null!;
    ArtifactRenderPlan _strict = null!;

    void Because()
    {
        _plan = event_context_projection_model.Plan(event_context_projection_model.UnselectedDegradable, slice: "Healthy");
        _strict = event_context_projection_model.Plan(event_context_projection_model.UnselectedDegradable, strict: true, slice: "Healthy");
    }

    [Fact] void should_succeed() => _plan.Success.ShouldBeTrue();
    [Fact] void should_not_warn_about_the_unselected_slice() => _plan.Diagnostics.Any(_ => _.Code == ArtifactRenderDegradationCodes.UnsupportedConstruct).ShouldBeFalse();
    [Fact] void should_not_record_a_degradation() => _plan.Degradations.IsEmpty.ShouldBeTrue();
    [Fact] void should_not_be_refused_when_strict() => _strict.Success.ShouldBeTrue();
}
