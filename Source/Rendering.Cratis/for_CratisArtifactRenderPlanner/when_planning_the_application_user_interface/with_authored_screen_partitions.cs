// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_the_application_user_interface.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRendering.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_the_application_user_interface;

/// <summary>
/// The pinned fingerprint is the plan the renderer produces with the scoped shared-UI capability removed, so an
/// application plan is unchanged by that capability. Re-derive it only by planning with that capability reverted.
/// </summary>
public class with_authored_screen_partitions : a_partitioned_screen_plan
{
    // Re-frozen for Cratis/Stage#757: the only changed artifact is package.json, which gains the @cratis/fundamentals resolution.
    // Re-captured on Train 8b: relative to main, only #660 adds CratisReadProtection On in Directory.Build.props; all other bytes match main.
    // Recomputed for Screenplay 4.254.0: the scaffold now emits the published matcher pin.
    // Recomputed for Chronicle 19.112.1, Arc 22.143.0 and Fundamentals 7.27.0: the scaffold now emits those package pins and the matching npm Arc pins.
    // Recomputed for the Microsoft.Extensions.Telemetry.Abstractions 10.11.0 pin (Cratis/Chronicle#5442): the backend scaffold now emits its central package version and the specifications project reference.
    // Re-frozen for #497's generated XML documentation and used-only specification imports;
    // Cratis.Specifications 5.1.0 and train 8b's Screenplay 4.256.0 / Arc 22.146.0 pins;
    // Components 4.37.0 and Scene 4.86.0 (the scaffold now emits those npm pins);
    // Components 4.38.1 (only the npm package pin changes).
    // Recomputed for merged Screenplay 4.258.0, Arc 22.146.0 and npm @cratis/scene 4.86.0.
    // Recomputed for Screenplay 4.259.0: the scaffold now emits the published matcher pin.
    // Re-frozen for Cratis/Stage#120 merged with train 10: authentication artifacts and host wiring on top of the train 10 output.
    // Re-frozen for Cratis/Stage#937: only Directory.Packages.props pins Pkcs to 10.0.14 and documents Xml runtime servicing.
    // Recomputed for Screenplay 5.0.0: the scaffold emits the 5.0.0 matcher pin.
    // Recomputed for Screenplay 5.1.0: the scaffold emits the 5.1.0 matcher pin.
    // Re-frozen after merging main (Screenplay 5.1.0) into train 10: the 4.259.0-era constants were stale.
    // Recaptured by the owning spec for train 11's merged pins and rendering deliveries.
    // Recomputed for Chronicle 19.117.0: the scaffold emits the matching package and development image pins.
    // Recomputed for Screenplay 5.3.1: the scaffold emits the matching matcher pin.
    // Recomputed for Cratis/Stage#967: the scaffold hosts Choreography 2.2.0 with Chronicle 19.124.0 and Arc 22.165.2.
    // Recomputed for Arc 22.166.0: the scaffold emits the matching backend and frontend pins.
    // Recomputed for Chronicle 19.129.0: the scaffold emits the matching package pins and development image tag.
    // Recomputed for Screenplay 5.10.0, Arc 22.168.0 and the main merge: the scaffold emits the matching pins, and npm Arc, Components and Scene at the published releases.
    // Recomputed for Chronicle 19.142.0 and Arc 22.176.0 on Screenplay 6.0.0 and Scene 5.0.0: the pins (Directory.Packages.props, docker-compose.yml, package.json) and any Scene 5 outlet shapes in the scene.
    // Recomputed for the Scene 5.1.0 pins.
    // Recomputed for Chronicle 19.144.0 and Screenplay 6.4.0: only the scaffold pins change.
    // Recomputed for Arc 22.182.0: only the Arc pins in Directory.Packages.props and package.json change.
    // Recomputed for Screenplay 6.14.0: only the Screenplay pins in Directory.Packages.props and the scaffold catalog change.
    const string Before = "ab981152c80da782595936fbe09544d63d12dd3bdc7b6cd279d716d0cc3e20a0";

    void Because() => _application = AppPlan(_profile);

    [Fact] void should_plan_the_application() => _application.Diagnostics.Any(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeFalse();
    [Fact] void should_compose_the_shared_user_interface() => _application.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain("scene.json");
    [Fact] void should_plan_the_same_bytes_as_before() => PlanFingerprint.Of(_application).ShouldEqual(Before);
}
