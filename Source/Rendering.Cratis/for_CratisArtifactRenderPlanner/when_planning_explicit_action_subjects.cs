// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Scene.Model.Elements;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.Scene;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_explicit_action_subjects : given.a_register_project_render_request
{
    [Theory]
    [InlineData("item")]
    [InlineData("screen")]
    public void should_carry_the_subject_into_the_planned_scene(string subject)
    {
        var action = SceneElementFactory.Component("register", "core:action", new Dictionary<string, object?>
        {
            ["command"] = _registerProject.Name,
            ["actionSubject"] = subject,
        });
        var screen = new global::Cratis.Scene.Model.Screens.Screen("Projects", "Default", new Dictionary<string, IReadOnlyList<SceneElement>> { ["content"] = [action] }, [], []);
        var profile = CratisRendering.CreateProfile("Projects", _options, new SceneApplication([], [], [], [], [], [screen]));

        var plan = _planner.Plan(_request with { Profile = profile });

        plan.Diagnostics.WithoutAuthenticatedDefaultNotes().ShouldBeEmpty();
        var scene = Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath == "src/stage-scene.json").Bytes.AsSpan());
        scene.ShouldContain($"\"actionSubject\":\"{subject}\"");
    }
}
