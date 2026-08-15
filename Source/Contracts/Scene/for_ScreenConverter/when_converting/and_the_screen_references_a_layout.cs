// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenConverter.when_converting;

public class and_the_screen_references_a_layout : Specification
{
    ScreenSyntax _syntax = null!;
    ScreenConversionResult _result = null!;

    void Establish()
    {
        var slot = new ScreenSlotSyntax(
            "sidebar",
            [new ScreenTitleSyntax("Details", SourceLocation.Start)],
            SourceLocation.Start);

        var layoutDirective = new ScreenLayoutSyntax("MasterDetail", [slot], SourceLocation.Start);

        _syntax = new("InvoiceDetails", null, [layoutDirective], SourceLocation.Start);
    }

    void Because() => _result = ScreenConverter.Convert(_syntax, [], []);

    [Fact] void should_reference_the_declared_layout_by_name() => _result.Screen.Layout.ShouldEqual("MasterDetail");
    [Fact] void should_not_synthesize_an_implicit_layout() => _result.ImplicitLayout.ShouldBeNull();
    [Fact] void should_fill_the_referenced_slot_with_its_content() => _result.Screen.SlotContent["sidebar"].Count.ShouldEqual(1);
}
