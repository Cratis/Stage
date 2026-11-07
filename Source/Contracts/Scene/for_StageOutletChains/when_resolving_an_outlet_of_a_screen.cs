// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Engine.Outlets;
using Cratis.Scene.Model.Outlets;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_StageOutletChains;

// Stage translates; Scene's engine resolves. What a screen's outlet shows comes from Scene's chain builder and EffectiveOutlets over the
// translated document and the translated levels.
public class when_resolving_an_outlet_of_a_screen : for_OutletConverter.given.a_translated_catalog
{
    EffectiveOutlet _toolbar = null!;
    EffectiveOutlet _rowActions = null!;
    EffectiveOutlet _detailTabs = null!;
    OutletChain _chain = null!;

    void Because()
    {
        _chain = StageOutletChains.Of(_application, "ProductCatalog");
        _toolbar = StageOutletChains.Resolve(_application, "ProductCatalog", "toolbar");
        _rowActions = StageOutletChains.Resolve(_application, "ProductCatalog", "rowActions");
        _detailTabs = StageOutletChains.Resolve(_application, "ProductCatalog", "detailTabs");
    }

    [Fact] void should_chain_the_levels_down_to_the_slice_and_then_the_screen() =>
        _chain.Chain.Select(level => $"{level.Kind}:{level.Name}").ShouldEqual(["Layout:Application", "Template:ListPage", "Module:Catalog", "Feature:Products", "Slice:ProductCatalog", "Screen:ProductCatalog"]);

    // An own item never goes above an inherited one: `before refresh` puts it first after the inherited items, with a warning.
    [Fact] void should_keep_the_added_button_below_the_inherited_one_and_leave_out_the_hidden_one() => _toolbar.Items.Select(item => item.Item.Id).ShouldEqual(["refresh", "add", "more"]);
    [Fact] void should_report_the_hidden_item_as_removed() => _toolbar.Removed.Single().Item.Id.ShouldEqual("export");
    [Fact] void should_warn_that_the_added_button_cannot_go_above_an_inherited_one() => _toolbar.Diagnostics.Single().Code.ShouldEqual(OutletDiagnosticCode.PlacementAboveInherited);
    [Fact] void should_resolve_the_kind_of_the_outlet() => _toolbar.Kind.ShouldEqual(OutletKind.Actions);
    [Fact] void should_resolve_the_row_actions_the_feature_adds() => _rowActions.Items.Select(item => item.Item.Id).ShouldEqual(["price", "withdraw"]);
    [Fact] void should_resolve_what_the_row_actions_act_on() => _rowActions.Subject.ShouldEqual(OutletSubject.Item);
    [Fact] void should_resolve_the_contributed_tab() => _detailTabs.Items.Select(item => item.Item.Id).ShouldEqual(["history"]);
    [Fact] void should_resolve_the_slot_the_tabs_own() => _detailTabs.Slot.ShouldEqual("detail");
}
