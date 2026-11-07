// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Outlets;
using Cratis.Scene.Model.Toolbar;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_OutletConverter;

// A dialog template's `outlet actions of actions` is the outlet Scene draws a dialog's buttons from (DialogActionsOutlet): a Screenplay 5
// dialog template could not declare outlets, so Stage refused them.
public class when_translating_a_dialog_actions_outlet : given.a_translated_catalog
{
    Outlet Actions => _application.DialogTemplates.Single(template => template.Name == "TaskDialog").Outlets!.Single();
    OutletButton Button(string id) => Actions.Defaults.OfType<OutletButton>().Single(button => button.Id == id);

    [Fact] void should_be_the_outlet_scene_draws_a_dialogs_buttons_from() => Actions.Name.ShouldEqual(DialogActionsOutlet.Name);
    [Fact] void should_be_an_outlet_of_actions() => Actions.Kind.ShouldEqual(OutletKind.Actions);
    [Fact] void should_be_in_the_footer() => Actions.Slot.ShouldEqual("footer");
    [Fact] void should_carry_both_buttons_in_order() => Actions.Defaults.Select(item => item.Id).ShouldEqual(["cancel", "save"]);
    [Fact] void should_close_with_the_cancel_button() => Button("cancel").BuiltIn.ShouldEqual(OutletBuiltInAction.Close);
    [Fact] void should_submit_with_the_save_button() => Button("save").BuiltIn.ShouldEqual(OutletBuiltInAction.Submit);
    [Fact] void should_carry_the_emphasis_of_the_save_button() => Button("save").Emphasis.ShouldEqual(ToolbarEmphasis.Primary);
    [Fact] void should_report_no_conversion_diagnostics() => _application.ConversionDiagnostics.ShouldBeEmpty();
}
