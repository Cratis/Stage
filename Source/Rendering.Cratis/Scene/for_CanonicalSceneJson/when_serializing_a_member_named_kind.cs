// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Xunit;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Rendering.Cratis.Scene.for_CanonicalSceneJson;

/// <summary>
/// A command or response member may be named <c language="json">kind</c>, so a contract carries <c language="json">{"kind": {"kind": "string"}}</c>.
/// Only a string <c language="json">kind</c> discriminates a literal; any other value is ordinary data and is serialized as it is.
/// </summary>
public class when_serializing_a_member_named_kind : Specification
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
            Properties = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["contract"] = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["kind"] = new Dictionary<string, object?>(StringComparer.Ordinal) { ["kind"] = "string" },
                    ["count"] = new Dictionary<string, object?>(StringComparer.Ordinal) { ["kind"] = 3 }
                },
                ["initial"] = new Dictionary<string, object?>(StringComparer.Ordinal) { ["kind"] = "literal" }
            }
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

    [Fact] void should_carry_the_object_valued_kind_member() => _json.ShouldContain("\"kind\":{\"kind\":\"string\"}");
    [Fact] void should_carry_a_number_valued_kind_unchanged() => _json.ShouldContain("\"count\":{\"kind\":3}");
    [Fact] void should_still_give_a_literal_without_a_value_an_explicit_null() => _json.ShouldContain("\"initial\":{\"kind\":\"literal\",\"value\":null}");
}
