// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_event_context_mappings;

public class and_the_property_is_a_query_filter : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = event_context_projection_model.Plan(event_context_projection_model.FilteredByContext);

    [Fact] void should_succeed() => _plan.Success.ShouldBeTrue();

    // Exercises native event-context mapping on a Uuid query-filter target, not DateTime query-filter support.
    [Fact] void should_render_the_correlation() => event_context_projection_model.Source(_plan).ShouldContain("ToEventContextProperty(c => c.CorrelationId)");
    [Fact] void should_not_degrade() => _plan.Diagnostics.Any(_ => _.Code == ArtifactRenderDegradationCodes.UnsupportedConstruct).ShouldBeFalse();
    [Fact] void should_compile_the_generated_backend() => event_context_projection_model.AssertCompiles(_plan);
}
