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
    // Re-frozen for Cratis/Stage#757: the only changed artifact is package.json, which gains the @cratis/fundamentals resolution.
    // Re-captured on Train 8b: relative to main, only #660 adds CratisReadProtection On in Directory.Build.props; all other bytes match main.
    // Recomputed for Screenplay 4.254.0: the scaffold now emits the published matcher pin.
    // Recomputed for Arc 22.146.0, Screenplay 4.255.0 and Specifications 4.5.0: the scaffold now emits those package pins and the matching npm Arc pins.
    const string Before = "dc1b27288331117849b554637f0c24af08bd0cd1f71b92fad28882127dd88966";

    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = Plan();

    [Fact] void should_plan_the_application() => _plan.Success.ShouldBeTrue();
    [Fact] void should_compose_the_shared_user_interface() => _plan.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain("src/stage.ts");
    [Fact] void should_plan_the_same_bytes_as_before() => PlanFingerprint.Of(_plan).ShouldEqual(Before);
}
