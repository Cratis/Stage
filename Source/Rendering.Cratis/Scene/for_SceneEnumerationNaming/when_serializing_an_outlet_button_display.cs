// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Scene.Model.Outlets;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Scene.for_SceneEnumerationNaming;

public class when_serializing_an_outlet_button_display : Specification
{
    JsonNode _wire = null!;

    void Because() => _wire = CanonicalSceneJson.ToNode(new
    {
        icon = new OutletButton { Id = "create", Title = "New work item", Icon = "add", Display = OutletButtonDisplay.Icon },
        title = new OutletButton { Id = "archive", Title = "Archive", Display = OutletButtonDisplay.Title },
        both = new OutletButton { Id = "print", Title = "Print", Display = OutletButtonDisplay.IconAndTitle }
    })!;

    [Fact] void should_spell_an_icon_only_display_as_the_typescript_model_does() => _wire["icon"]!["display"]!.GetValue<string>().ShouldEqual("icon");
    [Fact] void should_spell_a_title_only_display_as_the_typescript_model_does() => _wire["title"]!["display"]!.GetValue<string>().ShouldEqual("title");
    [Fact] void should_spell_an_icon_and_title_display_as_the_typescript_model_does() => _wire["both"]!["display"]!.GetValue<string>().ShouldEqual("iconAndTitle");
}
