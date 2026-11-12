// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Host.Extensions.Packages;
using Xunit;
using AnApplication = Cratis.Stage.Host.Extensions.given.an_extension_application;
using SceneElements = Cratis.Scene.Model.Elements;

namespace Cratis.Stage.Host.Extensions.Components.for_ExtensionSceneComponents;

public class when_placing_a_component_on_a_screen : AnApplication
{
    SceneElements.ExternalComponent _placed = null!;
    string _id = null!;

    async Task Because()
    {
        await LoadApplication();
        var catalog = new ExtensionComponentCatalog(ExtensionModelCatalogs.Placements(_uiContract, _modelPackages));
        var scene = ExtensionSceneComponents.Apply(_scene, catalog, _modelPackages);
        _placed = Components(scene).Single(component => component.ComponentName == ExtensionSceneComponents.ElementName);
        _id = _modelPackages.ForSystem("Notifier")!.Id;
    }

    [Fact] void should_render_it_as_an_extension_component() => _placed.ComponentName.ShouldEqual("Stage:extensionComponent");
    [Fact] void should_name_the_screen_the_system_and_the_component() => ((string)_placed.Properties["screen"]!, (string)_placed.Properties["system"]!, (string)_placed.Properties["component"]!).ShouldEqual(("NoticeBoard", "Notifier", "NoticeBoard"));
    [Fact] void should_name_its_kind() => _placed.Properties["kind"].ShouldEqual("react");
    [Fact] void should_address_the_frontend_of_the_locked_package() => _placed.Properties["module"].ShouldEqual($"extension-assets/{_id}/index.js");
    [Fact] void should_carry_the_abilities_the_contract_lets_it_invoke() => ((IReadOnlyList<string>)_placed.Properties["invokes"]!).ShouldContainOnly("Notify");
    [Fact] void should_carry_the_signals_the_contract_says_it_raises() => ((IReadOnlyList<string>)_placed.Properties["signals"]!).ShouldContainOnly("dismissed");
    [Fact] void should_give_it_the_properties_the_screen_set() => ((IReadOnlyDictionary<string, object?>)_placed.Properties["properties"]!)["title"].ShouldEqual("Today");
    [Fact] void should_point_at_the_placeholder_of_the_package() => _placed.Properties["placeholder"].ShouldEqual($"/stage/extensions/packages/{_id}/placeholders/NoticeBoard");

    static IEnumerable<SceneElements.ExternalComponent> Components(SceneApplication scene) =>
        scene.Screens.SelectMany(screen => screen.SlotContent.Values).SelectMany(Flatten);

    static IEnumerable<SceneElements.ExternalComponent> Flatten(IReadOnlyList<SceneElements.SceneElement> elements) =>
        elements.OfType<SceneElements.ExternalComponent>().SelectMany(component => new[] { component }.Concat(component.Slots.Values.SelectMany(Flatten)));
}
