// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Screens;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenConverter.when_converting;

public class and_the_screen_references_a_screen_template : Specification
{
    ScreenSyntax _syntax = null!;
    Screen _result = null!;

    void Establish()
    {
        var sidebar = new ScreenSlotSyntax(
            "sidebar",
            [new ScreenTitleSyntax("Details", SourceLocation.Start)],
            SourceLocation.Start);

        var main = new ScreenSlotSyntax(
            "main",
            [new ScreenActionSyntax("CancelInvoice", null, null, SourceLocation.Start)],
            SourceLocation.Start);

        var templateReference = new ScreenTemplateReferenceSyntax("MasterDetail", [sidebar, main], SourceLocation.Start);

        _syntax = new("InvoiceDetails", null, [templateReference], SourceLocation.Start);
    }

    void Because() => _result = ScreenConverter.Convert(_syntax, "AppShell", [], []);

    [Fact] void should_name_the_screen_template_it_fills() => _result.ScreenTemplate.ShouldEqual("MasterDetail");
    [Fact] void should_still_render_inside_the_resolved_application_layout() => _result.Layout.ShouldEqual("AppShell");
    [Fact] void should_fill_the_referenced_slot_with_its_content() => _result.SlotContent["sidebar"].Count.ShouldEqual(1);
    [Fact] void should_fill_every_referenced_slot() => _result.SlotContent.Keys.ShouldContainOnly("sidebar", "main");
    [Fact] void should_not_use_the_layouts_content_slot() => _result.SlotContent.ContainsKey(DefaultLayout.ContentSlotName).ShouldBeFalse();
}
