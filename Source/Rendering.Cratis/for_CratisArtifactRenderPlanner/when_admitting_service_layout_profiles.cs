// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_admitting_service_layout_profiles : a_register_project_render_request
{
    ArtifactRenderProfile _layoutProfile = null!;
    ArtifactRenderPlan _admitted = null!;
    ArtifactRenderPlan _changed = null!;
    ArtifactRenderPlan _duplicated = null!;
    ArtifactRenderPlan _withRepositoryRoot = null!;
    CratisPlanResult _disagreeing = null!;
    CratisPlanResult _matching = null!;

    void Because()
    {
        _layoutProfile = CratisRendering.CreateProfile(_model.Application.Name, _options with { ServiceLayout = true });
        _admitted = Plan(_layoutProfile.Inputs);
        var input = _layoutProfile.Inputs.Single(candidate => candidate.Name == ServiceLayoutInput.Name);
        var edited = ArtifactRenderInput.Create(input.Name, input.Version, [.. input.Bytes, (byte)'#']);
        _changed = Plan([.. _layoutProfile.Inputs.Select(candidate => candidate.Name == ServiceLayoutInput.Name ? edited : candidate)]);
        var second = ArtifactRenderInput.Create(input.Name, "2", [.. input.Bytes]);
        _duplicated = Plan([.. _layoutProfile.Inputs, second]);
        var root = CratisRendering.CreateProfile(_model.Application.Name, _options with { RepositoryRoot = true });
        _withRepositoryRoot = Plan([.. root.Inputs, input]);
        var plan = new CratisPlanOptions(_model.Application.Name, "Projects", "Projects");
        _disagreeing = CratisRendering.PlanScaffold(plan with { Profile = _layoutProfile });
        _matching = CratisRendering.PlanScaffold(plan with { ServiceLayout = true, Profile = _layoutProfile });
    }

    [Fact] void should_admit_a_profile_with_the_layout() => _admitted.Diagnostics.Select(_ => _.Code).ShouldNotContain("STAGE-CRATIS-001");
    [Fact] void should_plan_in_the_service_folder_from_an_admitted_profile() => _admitted.Artifacts.All(artifact => artifact.RelativePath.StartsWith("Source/Projects/", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_reject_changed_layout_bytes() => ShouldReject(_changed);
    [Fact] void should_reject_a_duplicate_layout() => ShouldReject(_duplicated);
    [Fact] void should_reject_the_layout_beside_the_repository_root() => ShouldReject(_withRepositoryRoot);
    [Fact] void should_refuse_plan_options_that_disagree_with_the_profile() => _disagreeing.Diagnostics.Select(_ => _.Code).ShouldContain("STAGE-CRATIS-001");
    [Fact] void should_accept_plan_options_that_agree_with_the_profile() => _matching.Success.ShouldBeTrue();

    ArtifactRenderPlan Plan(IEnumerable<ArtifactRenderInput> inputs) => _planner.Plan(_request with
    {
        Profile = ArtifactRenderProfile.Create(_layoutProfile.Target, _layoutProfile.TargetVersion, _layoutProfile.Renderer, _layoutProfile.RendererVersion, [.. inputs])
    });

    static void ShouldReject(ArtifactRenderPlan plan)
    {
        plan.Success.ShouldBeFalse();
        plan.Diagnostics.Select(_ => _.Code).ShouldContain("STAGE-CRATIS-001");
        plan.Artifacts.ShouldBeEmpty();
    }
}
