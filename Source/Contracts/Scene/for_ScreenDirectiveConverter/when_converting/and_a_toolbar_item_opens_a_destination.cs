// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Common;
using Cratis.Scene.Model.Elements;
using Cratis.Scene.Model.Outlets;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenDirectiveConverter.when_converting;

public class and_a_toolbar_item_opens_a_destination : Specification
{
    ExternalComponent _toolbar = null!;
    OutletButton[] _items = null!;

    void Because()
    {
        var toolbar = new ScreenToolbarSyntax(
            "main",
            [
                new ToolbarItemSyntax("edit", ToolbarItemKind.Action, "EditInvoice", SourceLocation.Start) { Label = "Edit" },
                new ToolbarItemSyntax("details", ToolbarItemKind.Navigate, "InvoiceDetails", SourceLocation.Start)
                {
                    Presentation = [new PresentationValueSyntax("mode", "icon", SourceLocation.Start)],
                    Parameters = [new ScreenNavigationParameterSyntax("invoiceId", new UiBindingSyntax(UiBindingKind.DataContext, "invoice.id", SourceLocation.Start), SourceLocation.Start)]
                },
                new ToolbarItemSyntax("help", ToolbarItemKind.Dialog, "HelpDialog", SourceLocation.Start)
            ],
            SourceLocation.Start);
        _toolbar = (ExternalComponent)ScreenDirectiveConverter.Convert([toolbar], "screen").Single();
        _items = [.. ((IEnumerable<OutletItem>)_toolbar.Properties["items"]!).Cast<OutletButton>()];
    }

    DestinationReference? Destination(int index) => _items[index].NavigateTo;

    [Fact] void should_place_an_outlet_of_actions_named_as_the_toolbar() => (_toolbar.ComponentName, _toolbar.Properties["name"]).ShouldEqual((ScreenDirectiveConverter.OutletItemsComponent, (object?)"main"));
    [Fact] void should_open_nothing_for_an_action() => Destination(0).ShouldBeNull();
    [Fact] void should_name_the_command_of_an_action() => _items[0].Command.ShouldEqual("EditInvoice");
    [Fact] void should_title_an_item_with_its_label() => _items[0].Title.ShouldEqual("Edit");
    [Fact] void should_title_an_item_without_a_label_with_its_name() => _items[1].Title.ShouldEqual("details");
    [Fact] void should_open_the_screen_a_navigate_item_targets() => Destination(1)!.Screen.ShouldEqual("InvoiceDetails");
    [Fact] void should_bind_the_parameters_of_a_navigate_item_as_route_parameters() => Destination(1)!.RouteParameterBindings!["invoiceId"].Path.ShouldEqual("invoice.id");
    [Fact] void should_open_the_dialog_a_dialog_item_targets() => string.Join('|', [Destination(2)!.Dialog, Destination(2)!.Kind]).ShouldEqual("HelpDialog|Dialog");
}
