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

public class and_a_toolbar_item_has_a_presentation : Specification
{
    static readonly SourceLocation _route = new(12, 13, "screen.play");
    readonly ConversionDiagnostics _diagnostics = new();
    OutletButton[] _items = null!;

    void Because()
    {
        var toolbar = new ScreenToolbarSyntax(
            "main",
            [
                new ToolbarItemSyntax("create", ToolbarItemKind.Action, "CreateInvoice", SourceLocation.Start)
                {
                    Presentation = [new PresentationValueSyntax("placement", "Primary", SourceLocation.Start)]
                },
                new ToolbarItemSyntax("archive", ToolbarItemKind.Action, "ArchiveInvoice", SourceLocation.Start)
                {
                    Presentation = [new PresentationValueSyntax("emphasis", "quiet", SourceLocation.Start)]
                },
                new ToolbarItemSyntax("refresh", ToolbarItemKind.Navigate, "Invoices", SourceLocation.Start)
                {
                    Presentation = [new PresentationValueSyntax("route", "invoices", _route)]
                }
            ],
            SourceLocation.Start);
        var toolbarComponent = (ExternalComponent)ScreenDirectiveConverter.Convert([toolbar], "screen", null, null, _diagnostics).Single();
        _items = [.. ((IEnumerable<OutletItem>)toolbarComponent.Properties["items"]!).Cast<OutletButton>()];
    }

    [Fact] void should_carry_a_primary_placement_as_the_emphasis() => _items[0].Emphasis.ShouldEqual(ToolbarEmphasis.Primary);
    [Fact] void should_carry_an_authored_emphasis() => _items[1].Emphasis.ShouldEqual(ToolbarEmphasis.Quiet);
    [Fact] void should_leave_an_item_without_an_emphasis_unemphasized() => _items[2].Emphasis.ShouldBeNull();
    [Fact] void should_report_only_the_presentation_it_cannot_carry() => _diagnostics.All.Count.ShouldEqual(1);
    [Fact] void should_report_the_omitted_presentation_as_a_warning() => (_diagnostics.All[0].Code, _diagnostics.All[0].Severity).ShouldEqual(("STAGE-SCENE-TOOLBAR-002", DiagnosticSeverity.Warning));
    [Fact] void should_report_where_the_omitted_presentation_was_authored() => (_diagnostics.All[0].Path, _diagnostics.All[0].Location).ShouldEqual(("screen.0-toolbar.refresh", _route));
}
