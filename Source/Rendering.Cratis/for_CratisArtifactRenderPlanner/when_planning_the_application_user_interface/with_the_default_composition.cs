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
    // Re-frozen for Cratis/Stage#870: the only changed artifact is the scaffolded Directory.Packages.props, which pins
    // Cratis.Screenplay.Matching at 4.251.0.
    const string Before = "896781b8c388d597b8ae6a2e375b1d9a54459f419156545021eb3129ad5973b4";

    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = CratisRendering.Plan(_model, _executionPlan, new(ArtifactRenderScopeKind.Application, _model.Application.Id), _options);

    [Fact] void should_plan_the_application() => _plan.Diagnostics.Any(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeFalse();
    [Fact] void should_compose_the_shared_user_interface() => _plan.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain("src/bindings.ts");
    [Fact] void should_plan_the_same_bytes_as_before() => PlanFingerprint.Of(_plan).ShouldEqual(Before);
}
