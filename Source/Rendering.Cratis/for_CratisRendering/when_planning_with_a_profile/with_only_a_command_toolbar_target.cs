// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Scene.Model.Profiles;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_with_a_profile;

public class with_only_a_command_toolbar_target : given.a_profile_source_plan
{
    JsonObject _runtime = null!;

    protected override SceneApplication? ComposedScene
    {
        get
        {
            var layout = DefaultLayout.Create();
            var button = SceneElementFactory.Component("register", "core:toolbarItem", new Dictionary<string, object?> { ["kind"] = "Action", ["label"] = "Register", ["target"] = "RegisterProject" });
            var screen = new SceneScreens.Screen("Projects", layout.Name, new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>(StringComparer.Ordinal) { ["content"] = [button] }, [], []);
            return new([new UiProfile("Web", "web", ["core"])], [], [layout], [], [], [screen]);
        }
    }

    void Establish() => PlanFull();

    void Because() => _runtime = JsonNode.Parse(Encoding.UTF8.GetString(_full.Artifacts.Single(artifact => artifact.RelativePath == StageSceneDocument.RelativePath).Bytes.AsSpan()))!.AsObject();

    [Fact] void should_plan_successfully() => _full.Success.ShouldBeTrue();
    [Fact] void should_contain_no_slice_button() => CanonicalSceneJson.NamesSlices(_runtime).ShouldBeFalse();
    [Fact] void should_not_add_unneeded_slice_structure() => _runtime.ContainsKey("structure").ShouldBeFalse();
    [Fact] void should_supply_the_target_command_schema() => (_runtime["commandSchemas"] is JsonObject schemas && schemas["RegisterProject"]?.GetValue<string>().Contains("\"properties\"", StringComparison.Ordinal) == true).ShouldBeTrue();
}
