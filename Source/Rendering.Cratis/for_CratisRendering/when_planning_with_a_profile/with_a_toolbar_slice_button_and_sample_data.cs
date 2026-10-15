// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Scene.Model.SampleData;
using Cratis.Scene.Model.Profiles;
using Cratis.Scene.Model.Structure;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_with_a_profile;

public class with_a_toolbar_slice_button_and_sample_data : given.a_profile_source_plan
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
            var profile = new UiProfile("Web", "web", ["core"], SampleData: new SampleDataSelection([], [], "nordic"));

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

    [Fact] void should_plan_successfully() => _full.Success.ShouldBeTrue();
    [Fact] void should_keep_the_sample_data_in_the_composed_scene_a_designer_edits() => _composed.ContainsKey("sampleDataPacks").ShouldBeTrue();
    [Fact] void should_carry_no_sample_data_packs_in_the_runtime_document() => _runtime.ContainsKey("sampleDataPacks").ShouldBeFalse();
    [Fact] void should_carry_no_sample_data_selection_in_the_runtime_profile() => _runtime["uiProfiles"]!.AsArray().Single()!.AsObject().ContainsKey("sampleData").ShouldBeFalse();
    [Fact] void should_carry_the_structure_the_slice_resolves_against() => _runtime["structure"]!["modules"]!.AsArray().Single()!["features"]!.AsArray().Single()!["slices"]!.AsArray().Single()!["id"]!.GetValue<string>().ShouldEqual("RegisterProject");
    [Fact] void should_carry_the_schema_of_the_command_the_slice_accepts() => _runtime["commandSchemas"]!["RegisterProject"]!.GetValue<string>().ShouldContain("\"properties\"");
}
