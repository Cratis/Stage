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
    // Recomputed for Screenplay 4.256.0: the scaffold now emits the published matcher pin.
    const string Before = "d86d3e0e5ec3ccce70af2129d46f3a48b113a9f84003f8f06d61b203bd19f767";

    void Because() => _application = AppPlan(_profile);

    [Fact] void should_plan_the_application() => _application.Diagnostics.Any(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeFalse();
    [Fact] void should_compose_the_shared_user_interface() => _application.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain("scene.json");
    [Fact] void should_plan_the_same_bytes_as_before() => PlanFingerprint.Of(_application).ShouldEqual(Before);
}
