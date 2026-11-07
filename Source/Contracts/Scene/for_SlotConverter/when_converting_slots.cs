// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;
using SceneLayouts = Cratis.Scene.Model.Layouts;

namespace Cratis.Stage.Contracts.Scene.for_SlotConverter;

public class when_converting_slots : Specification
{
    IReadOnlyList<SceneLayouts.Slot> _slots = null!;

    SceneLayouts.Slot Slot(string name) => _slots.Single(slot => slot.Name == name);

    void Because() => _slots = SlotConverter.Convert(
        [
            new SlotSyntax("content", SourceLocation.Start) { Role = SlotRole.Page, Routed = true, Primary = true },
            new SlotSyntax("detail", SourceLocation.Start) { Role = SlotRole.Pane, RoutedBy = "sku", Optional = true },
            new SlotSyntax("side", SourceLocation.Start) { Role = SlotRole.Panel },
            new SlotSyntax("header", SourceLocation.Start)
        ],
        [new TemplateSlotSyntax("nested", SourceLocation.Start), new TemplateSlotSyntax("content", SourceLocation.Start)]);

    [Fact] void should_keep_the_declared_order_and_add_the_fill_points_they_do_not_name() => _slots.Select(slot => slot.Name).ShouldEqual(["content", "detail", "side", "header", "nested"]);
    [Fact] void should_carry_a_page_role() => Slot("content").Role.ShouldEqual(SceneLayouts.SlotRole.Page);
    [Fact] void should_carry_a_pane_role() => Slot("detail").Role.ShouldEqual(SceneLayouts.SlotRole.Pane);
    [Fact] void should_carry_a_panel_role() => Slot("side").Role.ShouldEqual(SceneLayouts.SlotRole.Panel);
    [Fact] void should_leave_an_unwritten_role_for_scene_to_resolve() => Slot("header").Role.ShouldBeNull();
    [Fact] void should_carry_routed() => Slot("content").Routed.ShouldEqual(true);
    [Fact] void should_carry_primary() => Slot("content").Primary.ShouldEqual(true);
    [Fact] void should_route_a_slot_routed_by_a_parameter() => Slot("detail").Routed.ShouldEqual(true);
    [Fact] void should_carry_the_parameter_it_is_routed_by() => Slot("detail").RoutedBy!.Single().ShouldEqual("sku");
    [Fact] void should_carry_optional() => Slot("detail").Optional.ShouldEqual(true);
    [Fact] void should_leave_routed_unstated_when_unwritten() => Slot("header").Routed.ShouldBeNull();
    [Fact] void should_leave_primary_unstated_when_unwritten() => Slot("header").Primary.ShouldBeNull();
    [Fact] void should_leave_optional_unstated_when_unwritten() => Slot("header").Optional.ShouldBeNull();
    [Fact] void should_leave_routed_by_unstated_when_unwritten() => Slot("header").RoutedBy.ShouldBeNull();
    [Fact] void should_make_a_recursive_fill_point_a_routed_slot() => Slot("nested").Routed.ShouldEqual(true);
}
