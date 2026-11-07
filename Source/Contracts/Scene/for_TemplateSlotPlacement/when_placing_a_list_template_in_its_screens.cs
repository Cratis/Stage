// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_TemplateSlotPlacement;

public class when_placing_a_list_template_in_its_screens : given.a_translated_application
{
    const string Catalog =
        """
        module Catalog
          screen template ListPage
            category List
            header
            main
            detail pane routed by sku optional
            side panel routed
            tabbed pane routed
            outlet toolbar of actions in header
              default refresh title "Refresh" runs refresh
            outlet filters of filters in header
              default search title "Search"
            outlet stats of stats in header
            outlet sections of tabs in tabbed

          feature Products
            slice StateView ProductCatalog
              readmodel ProductRow
                sku String
                status String
              query AllProducts => ProductRow[]
                filter search String
              screen ProductCatalog
                template ListPage
                  main
                    table products
                      column sku
                      column status
              screen Shelves
                template ListPage
                  main
                    table shelves
                      column sku
                  detail
                    show ProductDetails by sku from shelves.selection

            slice StateView ProductDetails
              readmodel ProductDetail
                sku String
              query ProductBySku => ProductDetail
                by sku String
              screen ProductDetails
                data ProductDetail via query ProductBySku by sku
        """;

    void Establish() => Translate(Catalog);

    ExternalComponent[] In(string screen, string slot) =>
        [.. _application.Screens.Single(candidate => candidate.Name == screen).SlotContent.GetValueOrDefault(slot)?.OfType<ExternalComponent>() ?? []];

    ExternalComponent Show => In("Shelves", "detail").Single();

    ExternalComponent Detail => In("ProductCatalog", "detail").Single();

    [Fact] void should_place_the_outlets_the_template_draws_in_a_slot_at_its_start() =>
        In("ProductCatalog", "header").Select(element => (element.ComponentName, element.Properties["name"])).ShouldEqual([
            ("core:outletItems", (object?)"toolbar"), ("core:outletItems", "filters"), ("core:outletItems", "stats")]);
    [Fact] void should_place_an_outlet_of_tabs_in_the_slot_it_owns() =>
        In("ProductCatalog", "tabbed").Single().Properties["name"].ShouldEqual("sections");
    [Fact] void should_not_place_a_routed_slot_in_a_slot_tabs_own() =>
        In("ProductCatalog", "tabbed").Any(element => element.ComponentName == "core:slot").ShouldBeFalse();
    [Fact] void should_place_an_unfilled_routed_slot() => Detail.ComponentName.ShouldEqual("core:slot");
    [Fact] void should_name_an_unfilled_routed_slot() => Detail.Properties["name"].ShouldEqual("detail");
    [Fact] void should_give_an_unfilled_routed_slot_its_role() => Detail.Properties["role"].ShouldEqual("pane");
    [Fact] void should_give_an_unfilled_routed_slot_its_routing() => ((IEnumerable<string>)Detail.Properties["routedBy"]!).ShouldEqual(["sku"]);
    [Fact] void should_mark_an_unfilled_routed_slot_routed() => Detail.Properties["routed"].ShouldEqual(true);
    [Fact] void should_keep_an_unfilled_routed_slot_optional() => Detail.Properties["optional"].ShouldEqual(true);
    [Fact] void should_not_route_a_panel() => In("ProductCatalog", "side").ShouldBeEmpty();
    [Fact] void should_keep_what_the_screen_fills_a_slot_with() =>
        In("ProductCatalog", "main").Single().ComponentName.ShouldEqual("core:table");
    [Fact] void should_turn_a_show_line_into_the_slot_it_stands_in() => Show.ComponentName.ShouldEqual("core:slot");
    [Fact] void should_name_the_slot_a_show_line_stands_in() => Show.Properties["name"].ShouldEqual("detail");
    [Fact] void should_route_the_slot_a_show_line_stands_in_by_its_template() => ((IEnumerable<string>)Show.Properties["routedBy"]!).ShouldEqual(["sku"]);
    [Fact] void should_carry_the_screen_a_show_line_shows() => Show.Properties["shows"].ShouldEqual("ProductDetails");
    [Fact] void should_carry_the_parameter_a_show_line_keys_by() => Show.Properties["showsBy"].ShouldEqual("sku");
    [Fact] void should_carry_the_element_a_show_line_takes_the_selection_from() => Show.Properties["showsFrom"].ShouldEqual("shelves");
    [Fact] void should_resolve_the_element_whose_selection_a_show_line_shows() =>
        Show.Properties["showsFromElement"].ShouldEqual(In("Shelves", "main").Single().Id);
    [Fact] void should_report_no_conversion_diagnostics() => _application.ConversionDiagnostics.ShouldBeEmpty();
}
