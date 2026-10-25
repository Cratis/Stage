// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_shared_user_interface;

public class with_an_input_naming_an_unknown_slice : given.a_partitioned_screen_plan
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var profile = ArtifactRenderProfile.Create(_profile.Target, _profile.TargetVersion, _profile.Renderer, _profile.RendererVersion, [.. _profile.Inputs, UserInterfaceInput.Create(["not-a-slice"])]);
        _plan = new CratisArtifactRenderPlanner().Plan(new(_loaded.Model, _loaded.Plan, profile, new(ArtifactRenderScopeKind.Slice, Slice("RegisterProject").Id)));
    }

    [Fact] void should_plan_nothing() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_name_the_unknown_slice() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-SCREEN-007" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error && diagnostic.Message.Contains("'not-a-slice'", StringComparison.Ordinal)).ShouldBeTrue();
}
