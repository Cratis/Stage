// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_the_application_user_interface.given;
using Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_the_application_user_interface;

/// <summary>
/// The pinned fingerprint is the plan the renderer produces with the scoped shared-UI capability removed, so an
/// application plan is unchanged by that capability. Re-derive it only by planning with that capability reverted.
/// </summary>
public class with_an_authored_scene : a_screen_composition_render
{
    const string Before = "685a185adc2c5f7aa729de220b59baa2461123914c971279a7aee3941697c90b";

    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = Plan();

    [Fact] void should_plan_the_application() => _plan.Success.ShouldBeTrue();
    [Fact] void should_compose_the_shared_user_interface() => _plan.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain("src/stage.ts");
    [Fact] void should_plan_the_same_bytes_as_before() => PlanFingerprint.Of(_plan).ShouldEqual(Before);
}
