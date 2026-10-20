// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_admitting_repository_root_profiles : a_register_project_render_request
{
    const string WorkflowInput = "cratis-scaffold:text:.github/workflows/build.yml";

    ArtifactRenderProfile _rootProfile = null!;
    ArtifactRenderPlan _admitted = null!;
    ArtifactRenderPlan _changed = null!;
    ArtifactRenderPlan _missing = null!;
    CratisPlanResult _disagreeing = null!;
    CratisPlanResult _matching = null!;

    void Because()
    {
        _rootProfile = CratisRendering.CreateProfile(_model.Application.Name, _options with { RepositoryRoot = true });
        _admitted = Plan(_rootProfile.Inputs);
        var workflow = _rootProfile.Inputs.Single(input => input.Name == WorkflowInput);
        var edited = ArtifactRenderInput.Create(workflow.Name, workflow.Version, [.. workflow.Bytes, (byte)'#']);
        _changed = Plan([.. _rootProfile.Inputs.Select(input => input.Name == WorkflowInput ? edited : input)]);
        _missing = Plan([.. _rootProfile.Inputs.Where(input => input.Name != WorkflowInput)]);
        var plan = new CratisPlanOptions(_model.Application.Name, "Projects", "Projects");
        _disagreeing = CratisRendering.PlanScaffold(plan with { Profile = _rootProfile });
        _matching = CratisRendering.PlanScaffold(plan with { RepositoryRoot = true, Profile = _rootProfile });
    }

    [Fact] void should_admit_a_profile_with_workflows() => _admitted.Diagnostics.Select(_ => _.Code).ShouldNotContain("STAGE-CRATIS-001");
    [Fact] void should_plan_the_workflow_from_an_admitted_profile() => _admitted.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain(".github/workflows/build.yml");
    [Fact] void should_reject_changed_workflow_bytes() => ShouldReject(_changed);
    [Fact] void should_reject_a_workflow_without_its_dependency_updates() => ShouldReject(_missing);
    [Fact] void should_refuse_plan_options_that_disagree_with_the_profile() => _disagreeing.Diagnostics.Select(_ => _.Code).ShouldContain("STAGE-CRATIS-001");
    [Fact] void should_accept_plan_options_that_agree_with_the_profile() => _matching.Success.ShouldBeTrue();

    ArtifactRenderPlan Plan(IEnumerable<ArtifactRenderInput> inputs) => _planner.Plan(_request with
    {
        Profile = ArtifactRenderProfile.Create(_rootProfile.Target, _rootProfile.TargetVersion, _rootProfile.Renderer, _rootProfile.RendererVersion, [.. inputs])
    });

    static void ShouldReject(ArtifactRenderPlan plan)
    {
        plan.Success.ShouldBeFalse();
        plan.Diagnostics.Select(_ => _.Code).ShouldContain("STAGE-CRATIS-001");
        plan.Artifacts.ShouldBeEmpty();
    }
}
