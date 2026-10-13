// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Scene.Model.Forms;
using Cratis.Scene.Model.Screens;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Xunit;

namespace Cratis.Stage.Api.for_SceneSynthesizer;

// Studio places a command form among the elements of a screen as the Cratis components package's `commandForm`. The runtime
// shows it where it was placed, as the command form of its command, instead of dropping it or moving it after the other elements.
public class when_a_command_form_is_placed_on_a_screen : given.a_scene_model
{
    SceneApplication _result = null!;

    void Because()
    {
        var title = Element("title", "core:title", new Dictionary<string, object?> { ["text"] = "Register" });
        var form = Element("registration-form", "Cratis.Components.commandForm", new Dictionary<string, object?> { ["command"] = "RegisterProject", ["label"] = "Registration" });
        var cancel = Element("cancel", "core:action", new Dictionary<string, object?> { ["command"] = "RegisterProject", ["label"] = "Register" });
        var screen = new Screen(
            "ProjectRegistration",
            _scene.Layouts[0].Name,
            new Dictionary<string, IReadOnlyList<SceneElement>>(StringComparer.Ordinal) { [DefaultLayout.ContentSlotName] = [title, form, cancel] },
            [new Form("RegisterProjectForm", "RegisterProject", null, [], FormGenerationMode.Auto)],
            []);

        _result = SceneSynthesizer.Synthesize(_scene with { Screens = [screen] }, _semanticModel);
    }

    IReadOnlyList<ExternalComponent> Content => [.. _result.Screens.Single().SlotContent[DefaultLayout.ContentSlotName].OfType<ExternalComponent>()];

    ExternalComponent Placed => Content.Single(element => element.Id == "registration-form");

    [Fact] void should_keep_the_elements_in_the_order_they_were_placed() => Content.Select(element => element.Id).ShouldEqual(["title", "registration-form", "cancel"]);
    [Fact] void should_show_the_placed_form_as_the_runtime_command_form() => Placed.ComponentName.ShouldEqual("Stage:commandForm");
    [Fact] void should_carry_the_command_name() => Placed.Properties["command"].ShouldEqual("RegisterProject");
    [Fact] void should_keep_the_label_it_was_authored_with() => Placed.Properties["label"].ShouldEqual("Registration");
    [Fact] void should_carry_the_command_schema() => ((string)Placed.Properties[SceneSynthesizer.SchemaProperty]!).ShouldContain("\"projectId\"");
    [Fact] void should_generate_the_fields_from_the_command() => ((IReadOnlyList<FormField>)Placed.Properties["fields"]!).Select(field => field.Name).ShouldContainOnly(["projectId", "name", "stage", "description", "tags"]);
    [Fact] void should_mark_the_metadata_as_available() => Placed.Properties["metadataStatus"].ShouldEqual("available");
    [Fact] void should_not_show_the_form_of_the_command_a_second_time() => _result.Screens.Single().SlotContent.GetValueOrDefault("forms").ShouldBeNull();

    static ExternalComponent Element(string id, string componentName, Dictionary<string, object?> properties) => new()
    {
        Id = id,
        Name = id,
        ComponentName = componentName,
        Properties = properties,
        Slots = new Dictionary<string, IReadOnlyList<SceneElement>>()
    };
}
