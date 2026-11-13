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
    // Recomputed for Chronicle 19.112.1, Arc 22.143.0 and Fundamentals 7.27.0: the scaffold now emits those package pins and the matching npm Arc pins.
    // Recomputed for the Microsoft.Extensions.Telemetry.Abstractions 10.11.0 pin (537a7b80, Cratis/Chronicle#5442): the backend scaffold now emits its central package version and the specifications project reference.
    // Re-frozen for #497's generated XML documentation and used-only specification imports;
    // Cratis.Specifications 5.1.0 and train 8b's Screenplay 4.256.0 / Arc 22.146.0 pins;
    // Components 4.37.0 and Scene 4.86.0 (npm pins and changed authored Scene output);
    // Components 4.38.1 (only the npm package pin changes).
    // Recomputed for merged Screenplay 4.258.0 and npm @cratis/scene 4.86.0.
    // Recomputed for Screenplay 4.259.0: the scaffold now emits the published matcher pin.
    // Re-frozen for Cratis/Stage#209 on Scene 4.86.0 and Screenplay 4.259.0: native guarded-action serialization and restored guard/message bridges.
    // Re-frozen after train 11: authored submit continuations, response contracts/reference schemas and bundled runtime changes;
    // generated documentation/specification imports, Entra host artifacts/wiring/dependencies and Yarn preapprovals;
    // Components 4.38.1, Cratis.Specifications 5.1.0, Microsoft.Identity.Web 4.16.1 and the Pkcs 10.0.13 security pin.
    // Re-captured by execution for train 11 merge (#209/#857 with #925): native guards on Scene 4.87.0 and merged scaffold/runtime output.
    // Recomputed for Chronicle 19.117.0: the scaffold emits the matching package and development image pins.
    // Recomputed for Screenplay 5.3.1: the scaffold emits the matching matcher pin.
    // Recomputed for Cratis/Stage#967: Choreography hosting and updated backend/frontend pins.
    // Recomputed for Chronicle 19.129.0: the scaffold emits the matching package pins and development image tag.
    // Recomputed for Screenplay 5.10.0, Arc 22.168.0 and the main merge: the scaffold emits the matching pins, and npm Arc, Components and Scene at the published releases.
    // Recomputed for Chronicle 19.142.0 and Arc 22.176.0 on Screenplay 6.0.0 and Scene 5.0.0: the pins (Directory.Packages.props, docker-compose.yml, package.json) and any Scene 5 outlet shapes in the scene.
    // Recomputed for the Scene 5.1.0 pins.
    // Recomputed for Chronicle 19.144.0 and Screenplay 6.4.0: only the scaffold pins change.
    // Recomputed for Arc 22.182.0: only the Arc pins in Directory.Packages.props and package.json change.
    // Recomputed for Screenplay 6.14.0: only the Screenplay pins in Directory.Packages.props and the scaffold catalog change.
    // Recomputed for Arc 22.184.0: only the Arc pins in Directory.Packages.props and package.json change.
    // Recomputed for Screenplay 6.22.0, Choreography 2.5.0 and Chronicle 19.148.0: only the pins in Directory.Packages.props and the scaffold catalog change.
    // Recomputed for the Scene 5.3.0 pins and the @cratis/scene.extensions dependency of the Stage runtime.
    // Recomputed for Screenplay 6.24.0: only the Screenplay pins in Directory.Packages.props and the scaffold catalog change.
    // Recomputed for the Scene 5.4.0 pins and Cratis Components 4.40.1 that Scene 5.4.0 requires.
    const string Before = "35c81ce46a4e44a10883b56d8face2518db9477f05232055151cc8a864cdb5d0";

    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = Plan();

    [Fact] void should_plan_the_application() => _plan.Success.ShouldBeTrue();
    [Fact] void should_compose_the_shared_user_interface() => _plan.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain("src/stage.ts");
    [Fact] void should_plan_the_same_bytes_as_before() => PlanFingerprint.Of(_plan).ShouldEqual(Before);
}
