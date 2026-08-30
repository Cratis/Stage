// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_equivalent_facade_requests : a_register_project_render_request
{
    ArtifactRenderProfile _firstProfile = null!;
    ArtifactRenderProfile _secondProfile = null!;
    ArtifactRenderPlan _first = null!;
    ArtifactRenderPlan _second = null!;
    ArtifactRenderPlan _permuted = null!;

    void Because()
    {
        var scope = new ArtifactRenderScope(ArtifactRenderScopeKind.Slice, _registerProject.Id);
        _firstProfile = CratisRendering.CreateProfile(_model.Application.Name, _options);
        _secondProfile = CratisRendering.CreateProfile(_model.Application.Name, _options);
        _first = CratisRendering.Plan(_model, _executionPlan, scope, _options);
        _second = CratisRendering.Plan(_model, _executionPlan, scope, _options);

        var permutedProfile = ArtifactRenderProfile.Create(
            _firstProfile.Target,
            _firstProfile.TargetVersion,
            _firstProfile.Renderer,
            _firstProfile.RendererVersion,
            [.. _firstProfile.Inputs.Reverse()]);
        _permuted = _planner.Plan(new(_model, _executionPlan, permutedProfile, scope));
    }

    [Fact] void should_expose_the_single_v1_target() => CratisRendering.TargetId.ShouldEqual("cratis");
    [Fact] void should_expose_the_exact_target_version() => CratisRendering.TargetVersion.ShouldEqual("22.3.0");
    [Fact] void should_expose_the_exact_renderer_version() => CratisRendering.RendererVersion.ShouldEqual("1");
    [Fact] void should_create_identical_profile_identity_and_versions() => ProfileIdentity(_secondProfile).ShouldEqual(ProfileIdentity(_firstProfile));
    [Fact] void should_create_identical_profile_input_paths() => _secondProfile.Inputs.Select(_ => _.Name).SequenceEqual(_firstProfile.Inputs.Select(_ => _.Name)).ShouldBeTrue();
    [Fact] void should_create_identical_profile_input_bytes() => _secondProfile.Inputs.Zip(_firstProfile.Inputs).All(pair => pair.First.Bytes.SequenceEqual(pair.Second.Bytes)).ShouldBeTrue();
    [Fact] void should_create_identical_profile_input_hashes() => _secondProfile.Inputs.Select(_ => _.Sha256).SequenceEqual(_firstProfile.Inputs.Select(_ => _.Sha256)).ShouldBeTrue();
    [Fact] void should_plan_identical_artifact_paths() => _second.Artifacts.Select(_ => _.RelativePath).SequenceEqual(_first.Artifacts.Select(_ => _.RelativePath)).ShouldBeTrue();
    [Fact] void should_plan_identical_artifact_bytes() => _second.Artifacts.Zip(_first.Artifacts).All(pair => pair.First.Bytes.SequenceEqual(pair.Second.Bytes)).ShouldBeTrue();
    [Fact] void should_plan_identical_artifact_hashes() => _second.Artifacts.Select(_ => _.Sha256).SequenceEqual(_first.Artifacts.Select(_ => _.Sha256)).ShouldBeTrue();
    [Fact] void should_admit_a_permuted_profile_input_enumeration() => _permuted.Success.ShouldBeTrue();
    [Fact] void should_make_permuted_profile_inputs_produce_identical_paths() => _permuted.Artifacts.Select(_ => _.RelativePath).SequenceEqual(_first.Artifacts.Select(_ => _.RelativePath)).ShouldBeTrue();
    [Fact] void should_make_permuted_profile_inputs_produce_identical_bytes() => _permuted.Artifacts.Zip(_first.Artifacts).All(pair => pair.First.Bytes.SequenceEqual(pair.Second.Bytes)).ShouldBeTrue();
    [Fact] void should_make_permuted_profile_inputs_produce_identical_hashes() => _permuted.Artifacts.Select(_ => _.Sha256).SequenceEqual(_first.Artifacts.Select(_ => _.Sha256)).ShouldBeTrue();

    static string ProfileIdentity(ArtifactRenderProfile profile) => $"{profile.Target}|{profile.TargetVersion}|{profile.Renderer}|{profile.RendererVersion}";
}
