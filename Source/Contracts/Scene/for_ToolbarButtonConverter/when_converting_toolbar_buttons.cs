// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Engine.Toolbar;
using Cratis.Scene.Model.Elements;
using Cratis.Scene.Model.Outlets;
using Cratis.Scene.Model.Toolbar;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ToolbarButtonConverter;

// Scene 5: a screen's toolbar is the outlet of actions it places with core:outletItems, and every item and button is an outlet button.
public class when_converting_toolbar_buttons : given.a_translated_application
{
    const string Source =
        """
        module Shop
          feature Orders
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId Uuid identifier
                produces OrderPlaced
                  for orderId
                  orderId = orderId
              event OrderPlaced
                orderId Uuid
              screen PlaceOrder
                fits dialog
                action PlaceOrder
            slice StateView OrderList
              readmodel OrderRow
                orderId Uuid
              screen List
                title "Orders"
                toolbar actions
                  item legacy action PlaceOrder
                    label "Legacy"
                  button place title "Place order" slice Shop.Orders.PlaceOrder emphasis primary icon plus
                  button ret title $strings.orders.return slice StartReturn emphasis quiet
                  button idle title "Idle"
            slice Automation Notify
              reaction Notifier
                when OrderPlaced
          feature Returns
            slice StateChange StartReturn
              command StartReturn
                returnId Uuid identifier
                produces ReturnStarted
                  for returnId
                  returnId = returnId
              event ReturnStarted
                returnId Uuid
              screen StartReturn
                fits dialog
                action StartReturn
        """;

    ExternalComponent Toolbar => Descend(_application.Screens.Single(screen => screen.Name == "List").SlotContent.Values.SelectMany(content => content))
        .OfType<ExternalComponent>().Single(component => component.ComponentName == ScreenDirectiveConverter.OutletItemsComponent);
    OutletButton Button(string id) => ((IEnumerable<OutletItem>)Toolbar.Properties["items"]!).OfType<OutletButton>().Single(button => button.Id == id);

    static IEnumerable<SceneElement> Descend(IEnumerable<SceneElement> elements) =>
        elements.SelectMany(element => new[] { element }.Concat(element is ExternalComponent component ? Descend(component.Slots.Values.SelectMany(slot => slot)) : []));

    void Because() => Translate(Source);

    [Fact] void should_place_the_toolbar_as_an_outlet_of_actions() => (Toolbar.Properties["name"], Toolbar.Properties["kind"]).ShouldEqual(("actions", (object?)"actions"));
    [Fact] void should_carry_the_title() => Button("place").Title.ShouldEqual("Place order");
    [Fact] void should_carry_the_slice_as_its_qualified_id() => Button("place").Slice.ShouldEqual("Shop.Orders.PlaceOrder");
    [Fact] void should_resolve_a_bare_slice_name_to_its_qualified_id() => Button("ret").Slice.ShouldEqual("Shop.Returns.StartReturn");
    [Fact] void should_carry_the_emphasis() => Button("place").Emphasis.ShouldEqual(ToolbarEmphasis.Primary);
    [Fact] void should_carry_a_quiet_emphasis() => Button("ret").Emphasis.ShouldEqual(ToolbarEmphasis.Quiet);
    [Fact] void should_carry_the_icon() => Button("place").Icon.ShouldEqual("plus");
    [Fact] void should_keep_a_localized_title_as_written() => Button("ret").Title.ShouldEqual("$strings.orders.return");
    [Fact] void should_carry_no_slice_for_a_button_without_one() => Button("idle").Slice.ShouldBeNull();
    [Fact] void should_turn_a_command_item_into_a_button_naming_its_command() => (Button("legacy").Command, Button("legacy").Title).ShouldEqual(("PlaceOrder", "Legacy"));
    [Fact] void should_put_the_items_before_the_buttons() => ((IEnumerable<OutletItem>)Toolbar.Properties["items"]!).Select(item => item.Id).ShouldEqual(["legacy", "place", "ret", "idle"]);
    [Fact] void should_resolve_the_button_to_the_slice_command() =>
        ToolbarButtons.Resolve(new Cratis.Scene.Engine.Structure.ApplicationStructureIndex(_application.Structure), new ToolbarButton("place", "Place order", Button("place").Slice)).Command.ShouldEqual("PlaceOrder");
    [Fact] void should_report_only_the_missing_slice_warning() => RenderFindings.ForStructure(_application).Select(finding => finding.Message).Single().ShouldContain("ToolbarButtonSliceEmpty");
    [Fact] void should_report_the_slice_buttons() => _application.SliceButtons().Select(button => button.Id).ShouldContainOnly("place", "ret");
}
