// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Scene.Model.Screens;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenConverter.when_converting;

public class and_the_screen_has_no_template_directive : Specification
{
    ScreenSyntax _syntax = null!;
    Screen _result = null!;

    void Establish() => _syntax = new(
        "InvoiceList",
        null,
        [
            new ScreenDataSyntax(new TypeRefSyntax("InvoiceListReadModel", true, false, SourceLocation.Start), "ListInvoices", null, SourceLocation.Start),
            new ScreenActionSyntax("RegisterInvoice", null, null, SourceLocation.Start),
        ],
        SourceLocation.Start);

    void Because() => _result = ScreenConverter.Convert(_syntax, "AppShell", [], []);

    [Fact] void should_render_inside_the_resolved_application_layout() => _result.Layout.ShouldEqual("AppShell");
    [Fact] void should_fill_the_layouts_own_slots_rather_than_a_screen_templates() => _result.ScreenTemplate.ShouldBeNull();
    [Fact] void should_put_both_directives_in_the_content_slot() => _result.SlotContent[DefaultLayout.ContentSlotName].Count.ShouldEqual(2);
    [Fact] void should_convert_the_data_directive_to_a_core_data_component() => ((ExternalComponent)_result.SlotContent[DefaultLayout.ContentSlotName][0]).ComponentName.ShouldEqual("core:data");
    [Fact] void should_convert_the_action_directive_to_a_core_action_component() => ((ExternalComponent)_result.SlotContent[DefaultLayout.ContentSlotName][1]).ComponentName.ShouldEqual("core:action");
}
