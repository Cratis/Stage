// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Scene.Model.Common;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_StageSceneJson;

public class when_comparing_with_the_renderers_enumeration_naming : Specification
{
    string _served = null!;
    string _planned = null!;

    void Because()
    {
        var destination = new DestinationReference(Dialog: "EditDialog", Kind: DestinationKind.Dialog);
        _served = JsonSerializer.Serialize(destination, StageSceneJson.Options);
        _planned = System.Text.Json.Nodes.JsonNode.Parse(CanonicalSceneJson.ToNode(destination)!.ToJsonString())!.ToJsonString();
    }

    [Fact] void should_spell_the_pascal_cased_enumerations_as_the_renderer_does() => StageSceneJson.PascalCasedEnumerations.OrderBy(name => name, StringComparer.Ordinal).ShouldEqual(SceneEnumerationNaming.PascalCased.OrderBy(name => name, StringComparer.Ordinal));
    [Fact] void should_serve_a_destination_exactly_as_the_renderer_plans_it() => _served.ShouldEqual(_planned);
    [Fact] void should_spell_a_destination_kind_as_the_scene_model_does() => _served.ShouldContain("\"kind\":\"dialog\"");
    [Fact] void should_write_nothing_for_a_member_without_a_value() => _served.ShouldNotContain("null");
}
