// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_admitting_malformed_scene_inputs : a_register_project_render_request
{
    const string ValidScene = """
        {"uiProfiles":[],"themes":[],"layouts":[],"screenTemplates":[],"dialogTemplates":[],"screens":[{"name":"Projects","slotContent":{"content":[]}}]}
        """;
    readonly Dictionary<string, ArtifactRenderPlan> _rejected = [];
    ArtifactRenderPlan _accepted = null!;

    void Because()
    {
        var payloads = new Dictionary<string, string>
        {
            ["syntax"] = "{",
            ["null root"] = "null",
            ["array root"] = "[]",
            ["missing collections"] = "{}",
            ["null screen"] = ValidScene.Replace("[{\"name\":\"Projects\",\"slotContent\":{\"content\":[]}}]", "[null]", StringComparison.Ordinal),
            ["null slots"] = ValidScene.Replace("{\"content\":[]}", "null", StringComparison.Ordinal),
            ["non-array slot"] = ValidScene.Replace("\"content\":[]", "\"content\":{}", StringComparison.Ordinal),
            ["null element"] = ValidScene.Replace("\"content\":[]", "\"content\":[null]", StringComparison.Ordinal),
            ["duplicate member"] = ValidScene.Replace("\"content\":[]", "\"content\":[],\"content\":[]", StringComparison.Ordinal)
        };
        foreach (var (name, content) in payloads)
        {
            _rejected[name] = Plan(CratisArtifactRenderInput.CreateText("scene.json", CratisRendering.TargetVersion, content));
        }

        var valid = CratisArtifactRenderInput.CreateText("scene.json", CratisRendering.TargetVersion, ValidScene);
        _rejected["invalid UTF-8"] = Plan(ArtifactRenderInput.Create(valid.Name, valid.Version, [255]));
        _rejected["unrecognized version"] = Plan(ArtifactRenderInput.Create(valid.Name, "other", valid.Bytes));
        _accepted = Plan(valid);
    }

    [Fact] void should_exercise_every_rejection_case() => _rejected.Count.ShouldEqual(11);
    [Fact] void should_reject_invalid_compositions() => _rejected.Values.All(plan => !plan.Success).ShouldBeTrue();
    [Fact] void should_report_the_typed_profile_diagnostic_for_every_rejection() => _rejected.Values.All(plan => plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-CRATIS-001" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error)).ShouldBeTrue();
    [Fact] void should_return_no_candidate_artifacts_for_any_rejection() => _rejected.Values.All(plan => plan.Artifacts.IsEmpty).ShouldBeTrue();
    [Fact] void should_keep_a_well_formed_composition_admitted() => _accepted.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_authored_scene_bytes() => System.Text.Encoding.UTF8.GetString(_accepted.Artifacts.Single(artifact => artifact.RelativePath == "scene.json").Bytes.AsSpan()).ShouldEqual(ValidScene);

    ArtifactRenderPlan Plan(ArtifactRenderInput scene)
    {
        var profile = _request.Profile;
        return _planner.Plan(_request with
        {
            Profile = ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, [.. profile.Inputs, scene])
        });
    }
}
