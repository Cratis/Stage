// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_event_context_mappings;

public class and_the_mapping_is_a_plain_property : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = event_context_projection_model.Plan(event_context_projection_model.Plain);

    [Fact] void should_succeed() => _plan.Success.ShouldBeTrue();
    [Fact] void should_have_no_errors() => _plan.Diagnostics.Any(_ => _.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeFalse();
    [Fact] void should_not_degrade() => _plan.Degradations.IsEmpty.ShouldBeTrue();
    [Fact] void should_render_occurrence() => event_context_projection_model.Source(_plan).ShouldContain("ToEventContextProperty(c => c.Occurred)");
    [Fact] void should_render_correlation() => event_context_projection_model.Source(_plan).ShouldContain("ToEventContextProperty(c => c.CorrelationId)");
    [Fact] void should_not_comment_an_omitted_mapping() => event_context_projection_model.Source(_plan).ShouldNotContain("mapping omitted");
    [Fact] void should_keep_the_literal_mapping() => event_context_projection_model.Source(_plan).ShouldContain("\"global\"");
    [Fact] void should_compile_the_generated_backend() => event_context_projection_model.AssertCompiles(_plan);
}
