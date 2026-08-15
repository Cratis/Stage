// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenConverter.when_converting;

public class and_the_screen_has_no_layout_directive : Specification
{
    ScreenSyntax _syntax = null!;
    ScreenConversionResult _result = null!;

    void Establish() => _syntax = new(
        "InvoiceList",
        null,
        [
            new ScreenDataSyntax(new TypeRefSyntax("InvoiceListReadModel", true, false, SourceLocation.Start), "ListInvoices", null, SourceLocation.Start),
            new ScreenActionSyntax("RegisterInvoice", null, null, SourceLocation.Start),
        ],
        SourceLocation.Start);

    void Because() => _result = ScreenConverter.Convert(_syntax, [], []);

    [Fact] void should_reference_a_synthesized_implicit_layout() => _result.Screen.Layout.ShouldEqual("InvoiceList.implicit");
    [Fact] void should_return_the_synthesized_layout() => _result.ImplicitLayout.ShouldNotBeNull();
    [Fact] void should_name_the_synthesized_layout_the_same_as_the_screens_layout_reference() => _result.ImplicitLayout!.Name.ShouldEqual(_result.Screen.Layout);
    [Fact] void should_give_the_synthesized_layout_a_single_content_slot() => _result.ImplicitLayout!.Slots.Single().Name.ShouldEqual("content");
    [Fact] void should_put_both_directives_in_the_content_slot() => _result.Screen.SlotContent["content"].Count.ShouldEqual(2);
    [Fact] void should_convert_the_data_directive_to_a_core_data_component() => ((ExternalComponent)_result.Screen.SlotContent["content"][0]).ComponentName.ShouldEqual("core:data");
    [Fact] void should_convert_the_action_directive_to_a_core_action_component() => ((ExternalComponent)_result.Screen.SlotContent["content"][1]).ComponentName.ShouldEqual("core:action");
}
