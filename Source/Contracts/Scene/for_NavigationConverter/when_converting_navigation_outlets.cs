// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Common;
using Cratis.Scene.Model.Navigation;
using Cratis.Scene.Model.Outlets;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_NavigationConverter;

public class when_converting_navigation_outlets : given.a_translated_application
{
    const string Shell =
        """
        layout AppShell
          category application
          content page routed primary
          side panel

          outlet topbar of navigation
            logo image "/acme.svg" title "Acme"
            default sales title "Sales" icon receipt target Shop
            default support title $strings.nav.support target Support.Tickets slot side
          outlet sidebar of navigation in side
            default shop title "Shop" target Shop
              item orders title "Orders" target Shop.Orders
              item returns title "Returns" target Shop.Returns slot side

        module Shop
          outlet sidebar extends
            item more title "More" target Shop.Returns
          outlet topbar overrides
            item mine title "Mine" target Shop.Orders
          feature Orders
            slice StateView OrderList
              screen List
                title "Orders"
          feature Returns

        module Support
          outlet sidebar inherits
          feature Tickets

        module Empty
        """;

    NavigationModel Navigation => _application.Navigation;
    Outlet Outlet(string name) => Navigation.Application.Outlets.Single(outlet => outlet.Name == name);
    OutletNavigationItem Item(string outlet, int index) => (OutletNavigationItem)Outlet(outlet).Defaults[index];
    OutletRefinement Refinement(string module, string outlet) => Navigation.Modules.Single(candidate => candidate.Module == module).Refinements!.Single(refinement => refinement.Outlet == outlet);

    void Because() => Translate(Shell);

    [Fact] void should_convert_both_application_outlets() => Navigation.Application.Outlets.Select(outlet => outlet.Name).ShouldContainOnly("topbar", "sidebar");
    [Fact] void should_convert_them_as_outlets_of_navigation() => Navigation.Application.Outlets.All(outlet => outlet.Kind == OutletKind.Navigation).ShouldBeTrue();
    [Fact] void should_carry_the_slot_an_outlet_is_in() => Outlet("sidebar").Slot.ShouldEqual("side");
    [Fact] void should_convert_the_logo() => (Navigation.Application.Logo!.Image, Navigation.Application.Logo.Title).ShouldEqual(("/acme.svg", "Acme"));
    [Fact] void should_convert_the_item_title_icon_and_module_target() => (Item("topbar", 0).Title, Item("topbar", 0).Icon, Item("topbar", 0).Target).ShouldEqual(("Sales", "receipt", new DestinationReference(Module: "Shop")));
    [Fact] void should_convert_a_feature_target_and_slot() => (Item("topbar", 1).Target, Item("topbar", 1).Slot).ShouldEqual((new DestinationReference(Module: "Support", Feature: "Tickets"), "side"));
    [Fact] void should_keep_a_localized_title_as_written() => Item("topbar", 1).Title.ShouldEqual("$strings.nav.support");
    [Fact] void should_convert_sub_items() => Item("sidebar", 0).Children!.Select(item => item.Id).ShouldContainOnly("orders", "returns");
    [Fact] void should_leave_an_item_without_sub_items_with_none() => Item("topbar", 0).Children.ShouldBeNull();
    [Fact] void should_describe_only_the_modules_that_change_navigation() => Navigation.Modules.Select(module => module.Module).ShouldContainOnly("Shop", "Support");
    [Fact] void should_convert_an_extending_refinement() => Refinement("Shop", "sidebar").Mode.ShouldEqual(OutletMode.Extends);
    [Fact] void should_convert_an_overriding_refinement() => Refinement("Shop", "topbar").Mode.ShouldEqual(OutletMode.Overrides);
    [Fact] void should_convert_an_inheriting_refinement() => Refinement("Support", "sidebar").Mode.ShouldEqual(OutletMode.Inherits);
    [Fact] void should_convert_the_items_of_a_module_refinement() => Refinement("Shop", "sidebar").Items!.Single().Id.ShouldEqual("more");
    [Fact] void should_leave_an_inheriting_refinement_without_items() => Refinement("Support", "sidebar").Items.ShouldBeNull();
    [Fact] void should_report_no_findings() => Cratis.Stage.Contracts.Scene.RenderFindings.ForStructure(_application).ShouldBeEmpty();
}
