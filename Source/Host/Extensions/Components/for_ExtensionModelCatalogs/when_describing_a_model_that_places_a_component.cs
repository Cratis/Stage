// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;
using AnApplication = Cratis.Stage.Host.Extensions.given.an_extension_application;

namespace Cratis.Stage.Host.Extensions.Components.for_ExtensionModelCatalogs;

public class when_describing_a_model_that_places_a_component : AnApplication
{
    ExtensionComponentPlacement _placement = null!;
    ModelScreenExtensionUsage _usage = null!;

    async Task Because()
    {
        await LoadApplication();
        _placement = ExtensionModelCatalogs.Placements(_uiContract, _modelPackages).Single();
        _usage = ExtensionModelCatalogs.Usage(_uiContract);
    }

    [Fact] void should_place_it_on_the_screen_that_names_it() => _placement.Screen.ShouldEqual("NoticeBoard");
    [Fact] void should_place_the_component_of_the_system() => (_placement.System, _placement.Component).ShouldEqual(("Notifier", "NoticeBoard"));
    [Fact] void should_take_the_kind_from_the_contract() => _placement.Kind.ShouldEqual(ExtensionComponentKind.React);
    [Fact] void should_allow_the_abilities_the_contract_lets_it_invoke() => _placement.Invokes.ShouldContainOnly("Notify");
    [Fact] void should_allow_the_signals_the_contract_says_it_raises() => _placement.Signals.ShouldContainOnly("dismissed");
    [Fact] void should_need_the_system_on_the_screen() => _usage.SystemsOn("/NoticeBoard").ShouldContainOnly("Notifier");
    [Fact] void should_need_nothing_on_another_screen() => _usage.SystemsOn("/Elsewhere").ShouldBeEmpty();
}
