// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_event_context_mappings;

public class and_the_mapping_is_every_or_all : Specification
{
    ArtifactRenderPlan _plan = null!;
    void Because() => _plan = event_context_projection_model.Plan(a_native_context_model.Source, strict: true);

    [Fact] void should_succeed() => _plan.Success.ShouldBeTrue();
    [Fact] void should_not_degrade() => _plan.Degradations.IsEmpty.ShouldBeTrue();
    [Fact] void should_render_native_every() => event_context_projection_model.Source(_plan).ShouldContain("builder.FromEvery(every =>");
    [Fact] void should_render_native_all() => event_context_projection_model.Source(_plan).ShouldContain("builder.FromAll(every =>");
    [Fact] void should_render_occurrence() => event_context_projection_model.Source(_plan).ShouldContain("every.Set(model => model.Occurred).ToEventContextProperty(c => c.Occurred)");
    [Fact] void should_render_sequence() => event_context_projection_model.Source(_plan).ShouldContain("ToEventContextProperty(c => c.SequenceNumber)");
    [Fact] void should_render_source() => event_context_projection_model.Source(_plan).ShouldContain("Set(model => model.Source).ToEventSourceId()");
    [Fact] void should_compile() => event_context_projection_model.AssertCompiles(_plan);
}
