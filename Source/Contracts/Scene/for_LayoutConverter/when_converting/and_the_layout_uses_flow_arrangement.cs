// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Layouts;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_LayoutConverter.when_converting;

public class and_the_layout_uses_flow_arrangement : Specification
{
    LayoutSyntax _syntax = null!;
    Layout _result = null!;

    void Establish()
    {
        var root = new TemplateContainerSyntax(
            TemplateContainerKind.Row,
            [
                new TemplateSlotSyntax("sidebar", SourceLocation.Start, Grow: false, Span: null),
                new TemplateSlotSyntax("main", SourceLocation.Start, Grow: true, Span: null),
            ],
            SourceLocation.Start,
            Gap: 8);

        var template = new TemplateSyntax(root, [], SourceLocation.Start);

        _syntax = new(
            "MasterDetail",
            [new SlotSyntax("sidebar", null, SourceLocation.Start), new SlotSyntax("main", null, SourceLocation.Start)],
            SourceLocation.Start,
            LayoutArrangement.Flow,
            template);
    }

    void Because() => _result = LayoutConverter.Convert(_syntax);

    [Fact] void should_carry_the_layout_name() => _result.Name.ShouldEqual("MasterDetail");
    [Fact] void should_have_two_slots() => _result.Slots.Count.ShouldEqual(2);
    [Fact] void should_use_a_flow_arrangement() => _result.Arrangement.ShouldBeOfExactType<FlowArrangement>();

    [Fact]
    void should_arrange_the_slots_as_a_row_of_slot_leaves()
    {
        var flow = (FlowArrangement)_result.Arrangement!;
        var row = (FlowRow)flow.Root;
        row.Gap.ShouldEqual(8);
        row.Children.Count.ShouldEqual(2);
        ((FlowSlotLeaf)row.Children[0]).SlotName.ShouldEqual("sidebar");
        ((FlowSlotLeaf)row.Children[1]).SlotName.ShouldEqual("main");
    }

    [Fact] void should_carry_grow_onto_the_growing_slot_leaf() => Row.Children[1].Grow.ShouldEqual(1);
    [Fact] void should_leave_grow_unset_on_the_non_growing_slot_leaf() => Row.Children[0].Grow.ShouldBeNull();

    FlowRow Row => (FlowRow)((FlowArrangement)_result.Arrangement!).Root;
}
