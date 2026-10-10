// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication.given;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication;

/// <summary>
/// Planning the screen-composition corpus from its authored Scene with the guarded Close action still in it.
/// </summary>
/// <remarks>
/// The guarded action has no faithful runtime, so a generated application must not carry it, and a refused plan
/// must leave nothing behind: no partial application a caller could publish.
/// </remarks>
public class when_planning_the_screen_composition_with_its_guarded_actions : a_screen_composition_render
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = Plan(CanonicalSceneJson.Serialize(_scene));

    [Fact] void should_carry_the_guarded_action_in_the_authored_scene() => CanonicalSceneJson.Serialize(_scene).ShouldContain("\"otherwise\"");
    [Fact] void should_report_the_guarded_screen_action() => _plan.Diagnostics.Select(_ => _.Code).ShouldContainOnly(UnsupportedGuardedScreenAction.DiagnosticCode);
    [Fact] void should_not_succeed() => _plan.Success.ShouldBeFalse();
    [Fact] void should_plan_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
