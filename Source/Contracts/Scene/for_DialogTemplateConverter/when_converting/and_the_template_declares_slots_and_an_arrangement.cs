// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Layouts;
using Cratis.Scene.Model.Screens;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_DialogTemplateConverter.when_converting;

public class and_the_template_declares_slots_and_an_arrangement : Specification
{
    DialogTemplateSyntax _syntax = null!;
    DialogTemplate _result = null!;

    void Establish()
    {
        var root = new ArrangementContainerSyntax(
            ArrangementContainerKind.Column,
            [
                new ArrangementSlotSyntax("body", SourceLocation.Start, null, null, Grow: true, null),
                new ArrangementSlotSyntax("actions", SourceLocation.Start, null, Height: 48, Grow: false, null),
            ],
            SourceLocation.Start,
            Gap: 12);

        _syntax = new(
            "RegisterInvoiceDialog",
            [new SlotSyntax("body", null, SourceLocation.Start), new SlotSyntax("actions", null, SourceLocation.Start)],
            SourceLocation.Start,
            new ArrangementSyntax(ArrangementMode.Flow, SourceLocation.Start, root, [], null));
    }

    void Because() => _result = DialogTemplateConverter.Convert(_syntax);

    [Fact] void should_carry_the_template_name() => _result.Name.ShouldEqual("RegisterInvoiceDialog");
    [Fact] void should_carry_the_slots_it_offers() => _result.Slots.Select(slot => slot.Name).ShouldEqual(["body", "actions"]);
    [Fact] void should_use_a_flow_arrangement() => _result.Arrangement.ShouldBeOfExactType<FlowArrangement>();
    [Fact] void should_arrange_the_slots_as_a_column() => ((FlowColumn)((FlowArrangement)_result.Arrangement!).Root).Kind.ShouldEqual(FlowContainerKind.Column);
    [Fact] void should_provide_no_content_of_its_own() => _result.Content.ShouldBeNull();
}
