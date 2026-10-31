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
    // Recomputed for #494: commands and queries without an authorize clause render [Authorize] by default.
    const string Before = "9714b813a48ff94e987b13ff3174f5a95c7e0ee93cc9091fac58bfc6c499bd92";

    void Because() => _application = AppPlan(_profile);

    [Fact] void should_plan_the_application() => _application.Diagnostics.Any(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeFalse();
    [Fact] void should_compose_the_shared_user_interface() => _application.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain("scene.json");
    [Fact] void should_plan_the_same_bytes_as_before() => PlanFingerprint.Of(_application).ShouldEqual(Before);
}
