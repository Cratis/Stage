// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Common;
using Cratis.Scene.Model.Elements;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenDirectiveConverter.when_converting;

public class and_a_toolbar_item_opens_a_destination : Specification
{
    ExternalComponent[] _items = null!;

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

        var element = (ExternalComponent)ScreenDirectiveConverter.Convert([toolbar], "screen").Single();
        _items = [.. element.Slots["items"].Cast<ExternalComponent>()];
    }

    DestinationReference? Destination(int index) => (DestinationReference?)_items[index].Properties[SceneElementProperties.Destination];

    [Fact] void should_open_nothing_for_an_action() => Destination(0).ShouldBeNull();
    [Fact] void should_open_the_screen_a_navigate_item_targets() => Destination(1)!.Screen.ShouldEqual("InvoiceDetails");
    [Fact] void should_bind_the_parameters_of_a_navigate_item_as_route_parameters() => Destination(1)!.RouteParameterBindings!["invoiceId"].Path.ShouldEqual("invoice.id");
    [Fact] void should_open_the_dialog_a_dialog_item_targets() => string.Join('|', new object?[] { Destination(2)!.Dialog, Destination(2)!.Kind }).ShouldEqual("HelpDialog|Dialog");
    [Fact] void should_carry_the_presentation_hints_of_the_item() => ((Dictionary<string, string>)_items[1].Properties["presentation"]!)["mode"].ShouldEqual("icon");
}
