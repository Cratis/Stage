// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_the_application_user_interface.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRendering.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_the_application_user_interface;

/// <summary>
/// The pinned fingerprint is the plan the renderer produced before scoped plans could carry shared UI, so an
/// application plan is unchanged by that capability.
/// </summary>
public class with_authored_screen_partitions : a_partitioned_screen_plan
{
    const string Before = "9f4a328852d03d41e72bbd8d91a9244111885ee2cafb093a58d4c2cf017a9f04";

    void Because() => _application = AppPlan(_profile);

    [Fact] void should_plan_the_application() => _application.Diagnostics.Any(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeFalse();
    [Fact] void should_compose_the_shared_user_interface() => _application.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain("scene.json");
    [Fact] void should_plan_the_same_bytes_as_before() => PlanFingerprint.Of(_application).ShouldEqual(Before);
}
