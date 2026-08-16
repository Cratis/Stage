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
        var root = new ArrangementContainerSyntax(
            ArrangementContainerKind.Row,
            [
                new ArrangementSlotSyntax("navigation", SourceLocation.Start, Width: 240, Height: null, Grow: false, Span: null),
                new ArrangementSlotSyntax("content", SourceLocation.Start, Width: null, Height: null, Grow: true, Span: null),
            ],
            SourceLocation.Start,
            Gap: 8);

        _syntax = new(
            "AppShell",
            [new SlotSyntax("navigation", "Navigation", SourceLocation.Start), new SlotSyntax("content", null, SourceLocation.Start)],
            SourceLocation.Start,
            new ArrangementSyntax(ArrangementMode.Flow, SourceLocation.Start, root, [], null));
    }

    void Because() => _result = LayoutConverter.Convert(_syntax);

    [Fact] void should_carry_the_layout_name() => _result.Name.ShouldEqual("AppShell");
    [Fact] void should_have_two_slots() => _result.Slots.Count.ShouldEqual(2);
    [Fact] void should_use_a_flow_arrangement() => _result.Arrangement.ShouldBeOfExactType<FlowArrangement>();
    [Fact] void should_leave_every_slots_own_arrangement_unset() => _result.Slots.All(slot => slot.Arrangement is null).ShouldBeTrue();

    [Fact]
    void should_arrange_the_slots_as_a_row_of_slot_leaves()
    {
        Row.Gap.ShouldEqual(8);
        Row.Children.Count.ShouldEqual(2);
        ((FlowSlotLeaf)Row.Children[0]).SlotName.ShouldEqual("navigation");
        ((FlowSlotLeaf)Row.Children[1]).SlotName.ShouldEqual("content");
    }

    [Fact] void should_carry_grow_onto_the_growing_slot_leaf() => Row.Children[1].Grow.ShouldEqual(1);
    [Fact] void should_leave_grow_unset_on_the_non_growing_slot_leaf() => Row.Children[0].Grow.ShouldBeNull();
    [Fact] void should_carry_the_row_container_kind() => Row.Kind.ShouldEqual(FlowContainerKind.Row);

    FlowRow Row => (FlowRow)((FlowArrangement)_result.Arrangement!).Root;
}
