// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_the_application_user_interface.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_the_application_user_interface;

/// <summary>
/// The pinned fingerprint is the plan the renderer produces with the scoped shared-UI capability removed, so an
/// application plan is unchanged by that capability. Re-derive it only by planning with that capability reverted.
/// </summary>
public class with_the_default_composition : a_register_project_render_request
{
    // Re-frozen for Cratis/Stage#209: generated package.json pins Scene 4.84.0; capability-reverted bytes match.
    const string Before = "a8cad08b1ae8c62d625ee8fc03b3a417564bd2d474374d1327292b1e8f6c16b8";

    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = CratisRendering.Plan(_model, _executionPlan, new(ArtifactRenderScopeKind.Application, _model.Application.Id), _options);

    [Fact] void should_plan_the_application() => _plan.Diagnostics.Any(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeFalse();
    [Fact] void should_compose_the_shared_user_interface() => _plan.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain("src/bindings.ts");
    [Fact] void should_plan_the_same_bytes_as_before() => PlanFingerprint.Of(_plan).ShouldEqual(Before);
}
