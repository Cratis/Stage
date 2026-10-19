// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Engine.Toolbar;
using Cratis.Scene.Model.Elements;
using Cratis.Scene.Model.Toolbar;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ToolbarButtonConverter;

public class when_converting_toolbar_buttons : given.a_translated_application
{
    const string Source = "module Shop\n  feature Orders\n    slice StateChange PlaceOrder\n      command PlaceOrder\n        orderId Uuid identifier\n        produces OrderPlaced\n          orderId = orderId\n      event OrderPlaced\n        orderId Uuid\n    slice StateView OrderList\n      readmodel OrderRow\n        orderId Uuid\n      screen List\n        title \"Orders\"\n        toolbar actions\n          item legacy action PlaceOrder\n            label \"Legacy\"\n          button place title \"Place order\" slice Shop.Orders.PlaceOrder emphasis primary icon plus\n          button ret title $strings.orders.return slice StartReturn emphasis quiet\n          button idle title \"Idle\"\n    slice Automation Notify\n      reaction Notifier\n        when OrderPlaced\n  feature Returns\n    slice StateChange StartReturn\n      command StartReturn\n        returnId Uuid identifier\n        produces ReturnStarted\n          returnId = returnId\n      event ReturnStarted\n        returnId Uuid\n";

    ExternalComponent Item(string name) => Descend(_application.Screens.Single().SlotContent.Values.SelectMany(content => content))
        .OfType<ExternalComponent>().Single(component => component.ComponentName == "core:toolbarItem" && (string?)component.Properties["name"] == name);

    static IEnumerable<SceneElement> Descend(IEnumerable<SceneElement> elements) =>
        elements.SelectMany(element => new[] { element }.Concat(element is ExternalComponent component ? Descend(component.Slots.Values.SelectMany(slot => slot)) : []));

    ToolbarButton Button(string name) => ToolbarButtons.FromItem(Item(name).Id, Item(name).Properties)!;

    void Because() => Translate(Source);

    [Fact] void should_convert_each_button_to_a_toolbar_item() => Item("place").Properties["kind"].ShouldEqual("Action");
    [Fact] void should_carry_the_title_as_the_label() => Button("place").Title.ShouldEqual("Place order");
    [Fact] void should_carry_the_slice_as_its_qualified_id() => Button("place").Slice.ShouldEqual("Shop.Orders.PlaceOrder");
    [Fact] void should_resolve_a_bare_slice_name_to_its_qualified_id() => Button("ret").Slice.ShouldEqual("Shop.Returns.StartReturn");
    [Fact] void should_carry_the_emphasis() => Button("place").Emphasis.ShouldEqual(ToolbarEmphasis.Primary);
    [Fact] void should_carry_a_quiet_emphasis() => Button("ret").Emphasis.ShouldEqual(ToolbarEmphasis.Quiet);
    [Fact] void should_carry_the_icon() => Button("place").Icon.ShouldEqual("plus");
    [Fact] void should_keep_a_localized_title_as_written() => Button("ret").Title.ShouldEqual("$strings.orders.return");
    [Fact] void should_carry_no_slice_for_a_button_without_one() => Button("idle").Slice.ShouldBeNull();
    [Fact] void should_leave_the_legacy_item_a_command_item() => Item("legacy").Properties["kind"].ShouldEqual("Action");
    [Fact] void should_resolve_the_button_to_the_slice_command() => ToolbarButtons.Resolve(new Cratis.Scene.Engine.Structure.ApplicationStructureIndex(_application.Structure), Button("place")).Command.ShouldEqual("PlaceOrder");
    [Fact] void should_report_only_the_missing_slice_warning() => RenderFindings.ForStructure(_application).Select(finding => finding.Message).Single().ShouldContain("ToolbarButtonSliceEmpty");
}
