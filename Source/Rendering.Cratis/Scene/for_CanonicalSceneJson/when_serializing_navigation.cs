// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Xunit;
using SceneNavigation = Cratis.Scene.Model.Navigation;
using SceneOutlets = Cratis.Scene.Model.Outlets;
using SceneStructure = Cratis.Scene.Model.Structure;

namespace Cratis.Stage.Rendering.Cratis.Scene.for_CanonicalSceneJson;

/// <summary>
/// An application that declares no navigation outlet and no module navigation serializes as it did before navigation existed, and
/// without the structure nothing then resolves against. One navigation outlet carries both.
/// </summary>
public class when_serializing_navigation : Specification
{
    static readonly SceneStructure.ApplicationStructure _structure = new([new SceneStructure.ModuleNode("Projects", "Projects", [])]);

    JsonObject _withoutNavigation = null!;
    JsonObject _withNavigation = null!;

    void Because()
    {
        var application = new SceneApplication([], [], [DefaultLayout.Create()], [], [], []) { Structure = _structure };
        _withoutNavigation = JsonNode.Parse(CanonicalSceneJson.Serialize(application))!.AsObject();
        _withNavigation = JsonNode.Parse(CanonicalSceneJson.Serialize(application with
        {
            Navigation = new(new SceneNavigation.ApplicationNavigation([new SceneOutlets.Outlet("Navigation", SceneOutlets.OutletKind.Navigation, [], Slot: "navigation")]), [])
        }))!.AsObject();
    }

    [Fact] void should_omit_navigation_without_an_outlet() => _withoutNavigation.ContainsKey("navigation").ShouldBeFalse();
    [Fact] void should_omit_the_structure_nothing_resolves_against() => _withoutNavigation.ContainsKey("structure").ShouldBeFalse();
    [Fact] void should_carry_a_navigation_outlet() => _withNavigation["navigation"]!["application"]!["outlets"]!.AsArray().Count.ShouldEqual(1);
    [Fact] void should_carry_the_structure_navigation_resolves_against() => _withNavigation.ContainsKey("structure").ShouldBeTrue();
}
