// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Scene.Model.Profiles;
using Cratis.Scene.Model.SampleData;
using Cratis.Scene.Model.Structure;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_with_a_profile;

public class with_a_root_template_appearance_defaults_and_sample_data : given.a_profile_source_plan
{
    JsonObject _runtime = null!;
    JsonObject _composed = null!;

    protected override SceneApplication? ComposedScene
    {
        get
        {
            var layout = DefaultLayout.Create();
            var button = SceneElementFactory.Component("new", "core:toolbarItem", new Dictionary<string, object?> { ["kind"] = "Action", ["label"] = "Register", ["slice"] = "Projects.Registration.RegisterProject" });
            var screen = new SceneScreens.Screen("Projects", layout.Name, new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>(StringComparer.Ordinal) { ["content"] = [button] }, [], []);
            var profile = new UiProfile(
                "Web",
                "web",
                ["core"],
                SampleData: new SampleDataSelection([], [], "nordic"),
                Root: new RootTemplate(
                    new SceneScreens.TemplateReference(SceneScreens.TemplateReferenceSource.Blueprint, "AppShell", "Cratis.Blueprint.Default", "^1.0"),
                    new Dictionary<string, JsonNode?> { ["navigationMode"] = JsonValue.Create("slim"), ["showFooter"] = JsonValue.Create(false), ["showThemeSwitcher"] = JsonValue.Create(false) }),
                Appearance: new ProfileAppearance(ColorScheme.Dark, "#4f46e5"),
                Defaults: new ProfileDefaults(new SceneScreens.TemplateReference(SceneScreens.TemplateReferenceSource.Own, "MasterDetail")));

            return new([profile], [], [layout], [], [], [screen])
            {
                Structure = new([new("Projects", "Projects", [new("Registration", "Registration", [], [new("RegisterProject", "RegisterProject", SliceKind.StateChange, "RegisterProject")])])]),
                SampleDataPacks = [new("nordic", "Nordic", "nb-NO", "user", [])]
            };
        }
    }

    void Establish() => PlanFull();

    void Because()
    {
        _runtime = Document(StageSceneDocument.RelativePath);
        _composed = Document(SceneCompositionInput.RelativePath);
    }

    JsonObject Document(string path) => JsonNode.Parse(Encoding.UTF8.GetString(_full.Artifacts.Single(artifact => artifact.RelativePath == path).Bytes.AsSpan()))!.AsObject();

    JsonObject Profile(JsonObject document) => document["uiProfiles"]!.AsArray().Single()!.AsObject();

    [Fact] void should_plan_successfully() => _full.Success.ShouldBeTrue();
    [Fact] void should_carry_the_root_template_in_the_runtime_profile() => Profile(_runtime)["root"]!["template"]!["id"]!.GetValue<string>().ShouldEqual("AppShell");
    [Fact] void should_carry_the_blueprint_and_pin_of_the_root_template() => (Profile(_runtime)["root"]!["template"]!["blueprint"]!.GetValue<string>() + " " + Profile(_runtime)["root"]!["template"]!["version"]!.GetValue<string>()).ShouldEqual("Cratis.Blueprint.Default ^1.0");
    [Fact] void should_spell_the_root_source_the_way_the_scene_model_does() => Profile(_runtime)["root"]!["template"]!["source"]!.GetValue<string>().ShouldEqual("Blueprint");
    [Fact] void should_carry_the_shell_options_with_their_kinds() => Profile(_runtime)["root"]!["options"]!.ToJsonString().ShouldEqual("{\"navigationMode\":\"slim\",\"showFooter\":false,\"showThemeSwitcher\":false}");
    [Fact] void should_carry_the_appearance_the_way_the_scene_model_spells_it() => Profile(_runtime)["appearance"]!.ToJsonString().ShouldEqual("{\"accent\":\"#4f46e5\",\"colorScheme\":\"Dark\"}");
    [Fact] void should_carry_the_template_new_screens_start_from() => Profile(_runtime)["defaults"]!["newScreenTemplate"]!["id"]!.GetValue<string>().ShouldEqual("MasterDetail");
    [Fact] void should_keep_the_root_in_the_composed_scene_a_designer_edits() => Profile(_composed)["root"].ShouldNotBeNull();
    [Fact] void should_keep_the_sample_data_in_the_composed_scene_a_designer_edits() => _composed.ContainsKey("sampleDataPacks").ShouldBeTrue();
    [Fact] void should_carry_no_sample_data_packs_in_the_runtime_document() => _runtime.ContainsKey("sampleDataPacks").ShouldBeFalse();
    [Fact] void should_carry_no_sample_data_selection_in_the_runtime_profile() => Profile(_runtime).ContainsKey("sampleData").ShouldBeFalse();
}
