// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_admitting_a_profile : a_register_project_render_request
{
    ArtifactRenderPlan _wrongTarget = null!;
    ArtifactRenderPlan _wrongTargetVersion = null!;
    ArtifactRenderPlan _wrongRenderer = null!;
    ArtifactRenderPlan _wrongRendererVersion = null!;
    ArtifactRenderPlan _missingInput = null!;
    ArtifactRenderPlan _extraInput = null!;
    ArtifactRenderPlan _changedInput = null!;
    ArtifactRenderPlan _wrongInputVersion = null!;

    void Because()
    {
        var profile = _request.Profile;
        _wrongTarget = Plan(Profile(target: "other"));
        _wrongTargetVersion = Plan(Profile(targetVersion: "2"));
        _wrongRenderer = Plan(Profile(renderer: "other"));
        _wrongRendererVersion = Plan(Profile(rendererVersion: "2"));
        _missingInput = Plan(Profile(inputs: [.. profile.Inputs.Skip(1)]));
        _extraInput = Plan(Profile(inputs:
        [
            .. profile.Inputs,
            ArtifactRenderInput.Create("unexpected", "1", [1])
        ]));

        var first = profile.Inputs[0];
        var changed = ArtifactRenderInput.Create(first.Name, first.Version, [.. first.Bytes, (byte)'x']);
        _changedInput = Plan(Profile(inputs: [changed, .. profile.Inputs.Skip(1)]));
        var wrongVersion = ArtifactRenderInput.Create(first.Name, "2", first.Bytes);
        _wrongInputVersion = Plan(Profile(inputs: [wrongVersion, .. profile.Inputs.Skip(1)]));
    }

    [Fact] void should_reject_a_wrong_target_identity() => ShouldReject(_wrongTarget);
    [Fact] void should_reject_a_wrong_target_version() => ShouldReject(_wrongTargetVersion);
    [Fact] void should_reject_a_wrong_renderer_identity() => ShouldReject(_wrongRenderer);
    [Fact] void should_reject_a_wrong_renderer_version() => ShouldReject(_wrongRendererVersion);
    [Fact] void should_reject_a_missing_scaffold_input() => ShouldReject(_missingInput);
    [Fact] void should_reject_an_extra_scaffold_input() => ShouldReject(_extraInput);
    [Fact] void should_reject_changed_scaffold_bytes_and_hash() => ShouldReject(_changedInput);
    [Fact] void should_reject_a_wrong_scaffold_input_version() => ShouldReject(_wrongInputVersion);

    ArtifactRenderPlan Plan(ArtifactRenderProfile profile) => _planner.Plan(_request with { Profile = profile });

    ArtifactRenderProfile Profile(
        string? target = null,
        string? targetVersion = null,
        string? renderer = null,
        string? rendererVersion = null,
        System.Collections.Immutable.ImmutableArray<ArtifactRenderInput>? inputs = null) =>
        ArtifactRenderProfile.Create(
            target ?? _request.Profile.Target,
            targetVersion ?? _request.Profile.TargetVersion,
            renderer ?? _request.Profile.Renderer,
            rendererVersion ?? _request.Profile.RendererVersion,
            inputs ?? _request.Profile.Inputs);

    static void ShouldReject(ArtifactRenderPlan plan)
    {
        plan.Success.ShouldBeFalse();
        plan.Diagnostics.Select(_ => _.Code).ShouldContain("STAGE-CRATIS-001");
        plan.Artifacts.ShouldBeEmpty();
    }
}
