// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_guarded_scene_inputs : given.a_register_project_render_request
{
    [Theory]
    [InlineData("alternatives")]
    [InlineData("otherwise")]
    public void should_refuse_guarded_actions_supplied_as_raw_scene_json(string property)
    {
        var json = $$$$"""
            {"uiProfiles":[],"themes":[],"layouts":[],"screenTemplates":[],"dialogTemplates":[],"screens":[{"name":"Projects","slotContent":{"content":[{"componentName":"core:section","slots":{"content":[{"componentName":"core:action","properties":{"{{{{property}}}}":[]}}]}}]}}]}
            """;
        var input = CratisArtifactRenderInput.CreateText(SceneCompositionInput.RelativePath, CratisRendering.TargetVersion, json);
        var profile = _request.Profile;
        AssertRefused(_planner.Plan(_request with
        {
            Profile = ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, [.. profile.Inputs, input])
        }));
    }

    [Fact]
    public void should_refuse_manually_constructed_scenes_without_source_runtime_issues()
    {
        var action = SceneElementFactory.Component("register", "core:action", new Dictionary<string, object?> { ["otherwise"] = "hidden" });
        var screen = new global::Cratis.Scene.Model.Screens.Screen("Projects", "Default", new Dictionary<string, IReadOnlyList<SceneElement>> { ["content"] = [action] }, [], []);
        var scene = new SceneApplication([], [], [], [], [], [screen]);
        scene.RuntimeIssues.ShouldBeEmpty();
        var profile = CratisRendering.CreateProfile("Projects", _options, scene);
        AssertRefused(_planner.Plan(_request with { Profile = profile }));
    }

    static void AssertRefused(ArtifactRenderPlan plan)
    {
        plan.Success.ShouldBeFalse();
        plan.Artifacts.ShouldBeEmpty();
        var diagnostic = Assert.Single(plan.Diagnostics);
        diagnostic.Code.ShouldEqual(UnsupportedGuardedScreenAction.DiagnosticCode);
        diagnostic.Message.ShouldContain("https://github.com/Cratis/Scene/issues/68");
    }
}
