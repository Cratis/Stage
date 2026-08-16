// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.ContributionPoints;
using Cratis.Scene.Model.Elements;
using Cratis.Scene.Model.Layouts;
using Cratis.Scene.Model.Screens;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ComponentReferences.when_collecting;

public class and_components_are_nested_across_the_application : Specification
{
    SceneApplication _application = null!;
    IReadOnlyList<string> _result = null!;

    void Establish()
    {
        var screen = new Screen(
            "InvoiceDetails",
            "AppShell",
            new Dictionary<string, IReadOnlyList<SceneElement>>(StringComparer.Ordinal)
            {
                ["content"] =
                [
                    SceneElementFactory.Component(
                        "InvoiceDetails.0-section",
                        "core:section",
                        slots: new Dictionary<string, IReadOnlyList<SceneElement>>(StringComparer.Ordinal)
                        {
                            ["content"] = [SceneElementFactory.Component("InvoiceDetails.0-section.0-table", "core:table")],
                        }),

                    // The same name twice, so the result is a set rather than a tally.
                    SceneElementFactory.Component("InvoiceDetails.1-table", "core:table"),
                ],
            },
            [],
            [new Contribution("MainNavigation", SceneElementFactory.Component("InvoiceDetails.contribution", "core:contribution"))],
            "MasterDetail");

        var template = new ScreenTemplate(
            "MasterDetail",
            "content",
            [new Slot("main")],
            Content: new Dictionary<string, IReadOnlyList<SceneElement>>(StringComparer.Ordinal)
            {
                ["main"] = [SceneElementFactory.Component("MasterDetail.header", "blueprint:header")],
            });

        _application = new SceneApplication([], [], [], [template], [], [screen]);
    }

    void Because() => _result = ComponentReferences.Collect(_application);

    [Fact] void should_collect_a_component_a_screen_places_directly() => _result.ShouldContain("core:table");
    [Fact] void should_collect_a_component_nested_in_another_components_slot() => _result.ShouldContain("core:section");
    [Fact] void should_collect_a_component_a_contribution_carries() => _result.ShouldContain("core:contribution");
    [Fact] void should_collect_a_component_a_screen_templates_own_chrome_carries() => _result.ShouldContain("blueprint:header");
    [Fact] void should_name_each_component_once() => _result.Count.ShouldEqual(4);
    [Fact] void should_order_them_so_the_same_application_always_collects_the_same_list() => _result.SequenceEqual(["blueprint:header", "core:contribution", "core:section", "core:table"]).ShouldBeTrue();
}
