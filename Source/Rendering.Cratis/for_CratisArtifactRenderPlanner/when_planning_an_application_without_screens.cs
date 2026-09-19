// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// An application that composed no Scene has to plan exactly what it planned before composition existed.
/// </summary>
public class when_planning_an_application_without_screens : a_register_project_render_request
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_plan_without_errors() => _plan.Diagnostics.ShouldBeEmpty();

    [Fact] void should_not_carry_a_scene_payload() =>
        _plan.Artifacts.Any(_ => string.Equals(_.RelativePath, SceneCompositionInput.RelativePath, StringComparison.Ordinal)).ShouldBeFalse();

    [Fact] void should_not_emit_a_binding_module() =>
        _plan.Artifacts.Any(_ => string.Equals(_.RelativePath, SceneBindingsRenderer.RelativePath, StringComparison.Ordinal)).ShouldBeFalse();
}
