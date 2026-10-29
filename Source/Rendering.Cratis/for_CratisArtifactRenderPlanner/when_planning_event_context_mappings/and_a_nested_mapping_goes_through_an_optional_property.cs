// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_event_context_mappings;

public class and_a_nested_mapping_goes_through_an_optional_property : Specification
{
    ArtifactRenderPlan _plan = null!;
    ArtifactRenderPlan _strict = null!;

    void Because()
    {
        _plan = event_context_projection_model.Plan(event_context_projection_model.NestedThroughOptional);
        _strict = event_context_projection_model.Plan(event_context_projection_model.NestedThroughOptional, strict: true);
    }

    [Fact] void should_succeed() => _plan.Success.ShouldBeTrue();
    [Fact] void should_warn_once() => _plan.Diagnostics.Count(_ => _.Code == ArtifactRenderDegradationCodes.UnsupportedConstruct && _.Severity == ArtifactRenderDiagnosticSeverity.Warning).ShouldEqual(1);
    [Fact] void should_not_mention_event_context() => _plan.Diagnostics.Single(_ => _.Code == ArtifactRenderDegradationCodes.UnsupportedConstruct).Message.ShouldNotContain("eventContext");
    [Fact] void should_fail_when_strict() => _strict.Success.ShouldBeFalse();
}
