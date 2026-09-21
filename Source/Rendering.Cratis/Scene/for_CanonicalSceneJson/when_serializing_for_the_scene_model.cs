// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Xunit;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Rendering.Cratis.Scene.for_CanonicalSceneJson;

/// <summary>
/// The payload is read by the Scene model in TypeScript, so it has to carry that model's member names.
/// </summary>
/// <remarks>
/// TypeScript declares these members in camel case and decides what an element is by looking for
/// <c language="json">componentName</c> on it. A document carrying .NET member names parses perfectly well and then
/// matches nothing: every screen renders empty, and nothing reports an error.
/// </remarks>
public class when_serializing_for_the_scene_model : Specification
{
    string _json = null!;

    void Because()
    {
        var layout = DefaultLayout.Create();
        var element = new SceneElements.ExternalComponent
        {
            Id = "RegisterProject",
            Name = "RegisterProject",
            ComponentName = "Cratis.Components:commandForm",
            Properties = new Dictionary<string, object?>(StringComparer.Ordinal) { ["command"] = "RegisterProject" }
        };
        var screen = new SceneScreens.Screen(
            "Projects",
            layout.Name,
            new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>(StringComparer.Ordinal)
            {
                [DefaultLayout.ContentSlotName] = [element]
            },
            [],
            []);
        _json = CanonicalSceneJson.Serialize(new SceneApplication([], [], [layout], [], [], [screen]));
    }

    [Fact] void should_name_the_screens_as_the_model_declares_them() => _json.ShouldContain("\"screens\"");
    [Fact] void should_name_the_slot_content_as_the_model_declares_it() => _json.ShouldContain("\"slotContent\"");
    [Fact] void should_let_an_element_be_recognised_as_an_external_component() => _json.ShouldContain("\"componentName\"");
    [Fact] void should_carry_the_binding_name_the_registry_resolves() => _json.ShouldContain("\"command\":\"RegisterProject\"");
    [Fact] void should_not_carry_dotnet_member_names() => _json.ShouldNotContain("\"SlotContent\"");
    [Fact] void should_keep_the_slot_name_exactly_as_the_layout_declares_it() => _json.ShouldContain($"\"{DefaultLayout.ContentSlotName}\"");
}
