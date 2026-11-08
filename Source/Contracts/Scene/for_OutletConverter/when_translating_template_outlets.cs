// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Outlets;
using Cratis.Specifications;
using Xunit;
using SceneLayouts = Cratis.Scene.Model.Layouts;

namespace Cratis.Stage.Contracts.Scene.for_OutletConverter;

public class when_translating_template_outlets : given.a_translated_catalog
{
    Cratis.Scene.Model.Screens.ScreenTemplate ListPage => _application.ScreenTemplates.Single(template => template.Name == "ListPage");
    Outlet Outlet(string name) => ListPage.Outlets!.Single(outlet => outlet.Name == name);
    OutletButton Default(string id) => Outlet("toolbar").Defaults.OfType<OutletButton>().Single(button => button.Id == id);
    SceneLayouts.Slot Detail => ListPage.Slots.Single(slot => slot.Name == "detail");

    [Fact] void should_declare_every_outlet() => ListPage.Outlets!.Select(outlet => outlet.Name).ShouldEqual(["toolbar", "rowActions", "filters", "stats", "detailTabs"]);
    [Fact] void should_carry_an_outlet_of_actions() => Outlet("toolbar").Kind.ShouldEqual(OutletKind.Actions);
    [Fact] void should_carry_an_outlet_of_filters() => Outlet("filters").Kind.ShouldEqual(OutletKind.Filters);
    [Fact] void should_carry_an_outlet_of_stats() => Outlet("stats").Kind.ShouldEqual(OutletKind.Stats);
    [Fact] void should_carry_an_outlet_of_tabs() => Outlet("detailTabs").Kind.ShouldEqual(OutletKind.Tabs);
    [Fact] void should_carry_the_slot_an_outlet_is_in() => Outlet("toolbar").Slot.ShouldEqual("header");
    [Fact] void should_carry_the_slot_the_tabs_own() => Outlet("detailTabs").Slot.ShouldEqual("detail");
    [Fact] void should_carry_what_an_outlet_acts_on() => Outlet("rowActions").Subject.ShouldEqual(OutletSubject.Item);
    [Fact] void should_leave_the_subject_of_an_outlet_that_names_none_unstated() => Outlet("toolbar").Subject.ShouldBeNull();
    [Fact] void should_carry_the_defaults_in_order() => Outlet("toolbar").Defaults.Select(item => item.Id).ShouldEqual(["refresh", "export"]);
    [Fact] void should_carry_the_built_in_a_default_runs() => Default("refresh").BuiltIn.ShouldEqual(OutletBuiltInAction.Refresh);
    [Fact] void should_leave_a_default_without_a_hook_without_a_built_in() => Default("export").BuiltIn.ShouldBeNull();
    [Fact] void should_leave_a_default_without_a_hook_without_a_slice() => Default("export").Slice.ShouldBeNull();
    [Fact] void should_carry_the_role_of_a_slot() => Detail.Role.ShouldEqual(SceneLayouts.SlotRole.Pane);
    [Fact] void should_route_a_slot_routed_by_a_parameter() => Detail.Routed.ShouldEqual(true);
    [Fact] void should_carry_the_parameter_a_slot_is_routed_by() => Detail.RoutedBy!.Single().ShouldEqual("sku");
    [Fact] void should_carry_an_optional_slot() => Detail.Optional.ShouldEqual(true);
}
