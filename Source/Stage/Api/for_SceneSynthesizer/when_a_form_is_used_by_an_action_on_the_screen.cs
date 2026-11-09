// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Scene.Model.Forms;
using Cratis.Scene.Model.Screens;
using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Scene;
using Xunit;

namespace Cratis.Stage.Api.for_SceneSynthesizer;

public class when_a_form_is_used_by_an_action_on_the_screen : given.a_scene_model
{
    SceneApplication _result = null!;

    void Because()
    {
        var action = new ExternalComponent
        {
            Id = "register-project",
            Name = "Register project",
            ComponentName = "core:action",
            Properties = new Dictionary<string, object?> { ["command"] = "RegisterProject", ["label"] = "Register project" },
            Slots = new Dictionary<string, IReadOnlyList<SceneElement>>()
        };
        var screen = new Screen(
            "Projects",
            string.Empty,
            new Dictionary<string, IReadOnlyList<SceneElement>>(StringComparer.Ordinal) { [DefaultLayout.ContentSlotName] = [action] },
            [new Form("RegisterProjectForm", "RegisterProject", null, [new FormField("name", null, null, "Project name", null)], FormGenerationMode.Manual)],
            []);

        _result = SceneSynthesizer.Synthesize(_scene with { Screens = [screen] }, _semanticModel);
    }

    [Fact] void should_not_render_the_form_inline() => _result.Screens.Single().SlotContent.ContainsKey("forms").ShouldBeFalse();
    [Fact] void should_give_the_action_the_form_fields() => ((IReadOnlyList<FormField>)Action.Properties["fields"]!).Select(field => field.Label).ShouldContainOnly(["Project name"]);
    [Fact] void should_name_the_form_on_the_action() => Action.Properties["form"].ShouldEqual("RegisterProjectForm");

    ExternalComponent Action => _result.Screens.Single().SlotContent[DefaultLayout.ContentSlotName].OfType<ExternalComponent>().Single();
}
