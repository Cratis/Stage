// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Contracts.Scene.for_SceneApplication;

public class when_a_toolbar_button_names_a_slice : Specification
{
    static SceneApplication ApplicationWith(SceneElements.SceneElement element)
    {
        var layout = DefaultLayout.Create();
        var screen = new SceneScreens.Screen("Orders", layout.Name, new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>(StringComparer.Ordinal) { ["content"] = [element] }, [], []);
        return new([], [], [layout], [], [], [screen]);
    }

    static SceneElements.ExternalComponent Item(string? slice) =>
        SceneElementFactory.Component("new", "core:toolbarItem", slice is null ? [] : new Dictionary<string, object?> { ["slice"] = slice });

    [Fact] void should_report_it_for_a_button_on_a_screen() => ApplicationWith(Item("Sales.Orders.PlaceOrder")).ReferencesSlices.ShouldBeTrue();
    [Fact] void should_report_it_for_a_button_nested_in_a_toolbar() => ApplicationWith(SceneElementFactory.Component("toolbar", "core:toolbar", new Dictionary<string, object?>(), new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>> { ["items"] = [Item("Sales.Orders.PlaceOrder")] })).ReferencesSlices.ShouldBeTrue();
    [Fact] void should_not_report_it_for_a_button_without_a_slice() => ApplicationWith(Item(null)).ReferencesSlices.ShouldBeFalse();
    [Fact] void should_not_report_it_for_an_application_without_buttons() => ApplicationWith(SceneElementFactory.Component("title", "core:title", new Dictionary<string, object?>())).ReferencesSlices.ShouldBeFalse();
}
