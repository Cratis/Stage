// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Engine.Toolbar;
using Cratis.Scene.Model.Elements;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ToolbarButtonConverter;

// Cratis/Screenplay#1012: the buttons a feature declares are the buttons of the toolbar slot of the template its screens fill.
public class when_a_feature_declares_a_toolbar : given.a_translated_application
{
    const string Source =
        """
        module Shop
          screen template ListPage
            fits slot content
            header
            toolbar
            list
          screen template Plain
            fits slot content
            list
          feature Orders
            toolbar actions
              button place title "Place order" slice Shop.Orders.PlaceOrder emphasis primary icon plus
                confirm "Place it?"
              item cancel action CancelOrder
                label "Cancel order"
                confirm "Cancel it?"
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId Uuid identifier
                produces OrderPlaced
                  orderId = orderId
              event OrderPlaced
                orderId Uuid
            slice StateChange CancelOrder
              command CancelOrder
                orderId Uuid identifier
                produces OrderCancelled
                  orderId = orderId
              event OrderCancelled
                orderId Uuid
            slice StateView OrderList
              readmodel OrderRow
                orderId Uuid
              screen Orders
                template ListPage
                  toolbar
                    toolbar own
                      button mine title "Mine" slice Shop.Orders.PlaceOrder
            slice StateView OrderSummary
              readmodel SummaryRow
                orderId Uuid
              screen Summary
                template Plain
                  list
                    title "Summary"
        """;

    Cratis.Scene.Model.Screens.Screen Orders => _application.Screens.Single(screen => screen.Name == "Orders");
    Cratis.Scene.Model.Screens.Screen Summary => _application.Screens.Single(screen => screen.Name == "Summary");

    IEnumerable<ExternalComponent> ToolbarsOf(Cratis.Scene.Model.Screens.Screen screen) => screen.SlotContent["toolbar"].OfType<ExternalComponent>().Where(component => component.ComponentName == "core:toolbar");

    ExternalComponent Item(string name) => ToolbarsOf(Orders).SelectMany(toolbar => toolbar.Slots["items"]).OfType<ExternalComponent>().Single(item => (string?)item.Properties["name"] == name);

    void Because() => Translate(Source);

    [Fact] void should_put_the_screens_own_toolbar_first() => ToolbarsOf(Orders).Select(toolbar => (string?)toolbar.Properties["name"]).ShouldEqual(["own", "actions"]);
    [Fact] void should_add_the_feature_button() => Item("place").Properties["label"].ShouldEqual("Place order");
    [Fact] void should_link_the_button_to_the_slice_of_the_command() => ToolbarButtons.FromItem(Item("place").Id, Item("place").Properties)!.Slice.ShouldEqual("Shop.Orders.PlaceOrder");
    [Fact] void should_carry_the_confirmation_of_a_button() => Item("place").Properties["confirm"].ShouldEqual("Place it?");
    [Fact] void should_add_the_feature_item_linked_to_a_command() => (Item("cancel").Properties["target"], Item("cancel").Properties["confirm"]).ShouldEqual(("CancelOrder", "Cancel it?"));
    [Fact] void should_not_put_a_toolbar_in_a_template_without_a_toolbar_slot() => Summary.SlotContent.ContainsKey("toolbar").ShouldBeFalse();
}
