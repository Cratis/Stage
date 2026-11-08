// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Scene.Model.Outlets;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;
using ToolbarEmphasis = Cratis.Scene.Model.Toolbar.ToolbarEmphasis;

namespace Cratis.Stage.Contracts.Scene.for_ScreenDirectiveConverter.when_converting;

public class and_a_toolbar_item_has_a_display_mode : Specification
{
    static readonly SourceLocation _unknown = new(14, 17, "screen.play");
    readonly ConversionDiagnostics _diagnostics = new();
    OutletButton[] _items = null!;

    void Because()
    {
        var toolbar = new ScreenToolbarSyntax(
            "main",
            [
                new ToolbarItemSyntax("create", ToolbarItemKind.Dialog, "EditDialog", SourceLocation.Start)
                {
                    Label = "New work item",
                    Icon = "add",
                    Presentation = [new PresentationValueSyntax("mode", "icon", SourceLocation.Start)]
                },
                new ToolbarItemSyntax("archive", ToolbarItemKind.Action, "ArchiveInvoice", SourceLocation.Start)
                {
                    Icon = "archive",
                    Presentation =
                    [
                        new PresentationValueSyntax("mode", "Title", SourceLocation.Start),
                        new PresentationValueSyntax("placement", "quiet", SourceLocation.Start)
                    ]
                },
                new ToolbarItemSyntax("print", ToolbarItemKind.Action, "PrintInvoice", SourceLocation.Start)
                {
                    Presentation = [new PresentationValueSyntax("mode", "iconAndTitle", SourceLocation.Start)]
                },
                new ToolbarItemSyntax("refresh", ToolbarItemKind.Navigate, "Invoices", SourceLocation.Start)
                {
                    Presentation = [new PresentationValueSyntax("mode", "badge", _unknown)]
                }
            ],
            SourceLocation.Start);
        var toolbarComponent = (ExternalComponent)ScreenDirectiveConverter.Convert([toolbar], "screen", null, null, _diagnostics).Single();
        _items = [.. ((IEnumerable<OutletItem>)toolbarComponent.Properties["items"]!).Cast<OutletButton>()];
    }

    [Fact] void should_show_only_the_icon_for_an_icon_mode() => _items[0].Display.ShouldEqual(OutletButtonDisplay.Icon);
    [Fact] void should_keep_the_label_as_the_accessible_name_of_an_icon_only_item() => (_items[0].Title, _items[0].Icon).ShouldEqual(("New work item", "add"));
    [Fact] void should_show_only_the_title_for_a_title_mode() => _items[1].Display.ShouldEqual(OutletButtonDisplay.Title);
    [Fact] void should_carry_the_emphasis_alongside_the_mode() => _items[1].Emphasis.ShouldEqual(ToolbarEmphasis.Quiet);
    [Fact] void should_show_both_for_an_icon_and_title_mode() => _items[2].Display.ShouldEqual(OutletButtonDisplay.IconAndTitle);
    [Fact] void should_leave_an_unknown_mode_to_the_default() => _items[3].Display.ShouldBeNull();
    [Fact] void should_report_only_the_unknown_mode() => _diagnostics.All.Count.ShouldEqual(1);
    [Fact] void should_report_the_unknown_mode_as_an_omitted_presentation() => (_diagnostics.All[0].Code, _diagnostics.All[0].Severity, _diagnostics.All[0].Location).ShouldEqual(("STAGE-SCENE-TOOLBAR-002", DiagnosticSeverity.Warning, _unknown));
}
