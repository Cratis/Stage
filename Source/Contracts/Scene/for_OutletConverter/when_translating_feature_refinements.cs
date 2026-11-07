// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Engine.Outlets;
using Cratis.Scene.Model.Outlets;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_OutletConverter;

public class when_translating_feature_refinements : given.a_translated_catalog
{
    OutletLevel Products => _application.OutletLevels!.Single(level => level.Kind == OutletLevelKind.Feature && level.Name == "Products");
    OutletRefinement Refinement(string outlet) => Products.Refinements!.Single(refinement => refinement.Outlet == outlet);
    OutletItem Item(string outlet, string id) => Refinement(outlet).Items!.Single(item => item.Id == id);
    OutletButton Button(string outlet, string id) => (OutletButton)Item(outlet, id);

    [Fact] void should_make_the_feature_an_outlet_level() => Products.Kind.ShouldEqual(OutletLevelKind.Feature);
    [Fact] void should_refine_every_outlet_the_feature_names() => Products.Refinements!.Select(refinement => refinement.Outlet).ShouldEqual(["toolbar", "rowActions", "filters", "stats"]);
    [Fact] void should_carry_the_mode() => Refinement("toolbar").Mode.ShouldEqual(OutletMode.Extends);
    [Fact] void should_carry_the_ids_a_level_hides() => Refinement("toolbar").Hide!.Single().ShouldEqual("export");
    [Fact] void should_carry_where_an_item_goes() => Item("toolbar", "add").Before.ShouldEqual("refresh");
    [Fact] void should_write_the_slice_of_a_button_as_its_qualified_id() => Button("toolbar", "add").Slice.ShouldEqual("Catalog.Products.DraftProduct");
    [Fact] void should_carry_a_group_and_its_buttons() => ((OutletGroup)Item("toolbar", "more")).Items.Select(item => item.Id).ShouldEqual(["retire"]);
    [Fact] void should_carry_the_title_and_icon_of_a_group() => (((OutletGroup)Item("toolbar", "more")).Title, ((OutletGroup)Item("toolbar", "more")).Icon).ShouldEqual(("More", "dots"));
    [Fact] void should_open_a_hook_in_the_slot_it_names() => (Button("rowActions", "price").Opens, Button("rowActions", "price").OpensIn).ShouldEqual(((Opens?)Opens.Slot, "detail"));
    [Fact] void should_run_a_hook_that_runs_directly() => Button("rowActions", "withdraw").Opens.ShouldEqual(Opens.None);
    [Fact] void should_carry_when_a_hook_is_enabled() => Button("rowActions", "withdraw").EnabledWhen.ShouldEqual("item.status == \"onSale\"");
    [Fact] void should_carry_the_confirmation_of_a_hook() => Button("rowActions", "withdraw").Confirm.ShouldEqual("Withdraw this product?");
    [Fact] void should_leave_a_hook_without_opens_automatic() => Button("toolbar", "add").Opens.ShouldBeNull();
    [Fact] void should_carry_the_query_parameter_a_filter_sets() => ((OutletFilter)Item("filters", "status")).Sets.ShouldEqual(new QueryParameterReference("AllProducts", "status"));
    [Fact] void should_carry_the_query_field_a_stat_shows() => ((OutletStat)Item("stats", "drafts")).ValueFrom.ShouldEqual(new QueryFieldReference("ProductStats", "drafts"));
    [Fact] void should_carry_a_contribution_to_an_outlet() => Products.Contributions!.Single().Outlet.ShouldEqual("detailTabs");
    [Fact] void should_carry_the_tab_a_contribution_adds() => ((OutletTab)Products.Contributions!.Single().Items.Single()).Shows.ShouldEqual("ProductHistory");
    [Fact] void should_make_a_slice_screen_that_refines_an_outlet_a_slice_level() =>
        _application.OutletLevels!.Single(level => level.Kind == OutletLevelKind.Slice).Refinements!.Single().Mode.ShouldEqual(OutletMode.Inherits);
    [Fact] void should_name_the_slice_level_after_its_screen() => _application.OutletLevels!.Single(level => level.Kind == OutletLevelKind.Slice).Name.ShouldEqual("ProductCatalog");
    [Fact] void should_carry_where_a_screen_fits_in_order() =>
        _application.Screens.Single(screen => screen.Name == "SetProductPrice").Fits!.Select(fit => fit.ToString()).ShouldEqual(["Pane", "Drawer"]);
    [Fact] void should_leave_the_fits_of_a_screen_that_names_none_unstated() => _application.Screens.Single(screen => screen.Name == "ProductCatalog").Fits.ShouldBeNull();
    [Fact] void should_have_no_module_level_when_no_module_changes_an_outlet() => _application.OutletLevels!.Any(level => level.Kind == OutletLevelKind.Module).ShouldBeFalse();
}
