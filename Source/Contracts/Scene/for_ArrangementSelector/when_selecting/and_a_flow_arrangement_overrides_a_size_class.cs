// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Layouts;
using Cratis.Scene.Model.SizeClasses;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ArrangementSelector.when_selecting;

public class and_a_flow_arrangement_overrides_a_size_class : Specification
{
    FlowArrangement _arrangement = null!;
    ArrangementSelection? _wide;
    ArrangementSelection? _narrow;

    void Establish() => _arrangement = new FlowArrangement(
        new FlowRow { Children = [new FlowSlotLeaf("navigation")] },
        [new FlowOverride(WidthSizeClass.Compact, null, new FlowColumn { Children = [new FlowSlotLeaf("navigation")] })]);

    void Because()
    {
        _wide = ArrangementSelector.Select("AppShell", null, _arrangement, new SizeClass(WidthSizeClass.Regular, HeightSizeClass.Regular));
        _narrow = ArrangementSelector.Select("AppShell", null, _arrangement, new SizeClass(WidthSizeClass.Compact, HeightSizeClass.Regular));
    }

    [Fact] void should_select_the_root_tree_when_no_override_matches() => _wide!.Flow.ShouldBeOfExactType<FlowRow>();
    [Fact] void should_select_the_overriding_tree_when_one_matches() => _narrow!.Flow.ShouldBeOfExactType<FlowColumn>();
    [Fact] void should_name_the_structure_the_arrangement_belongs_to() => _wide!.Structure.ShouldEqual("AppShell");
    [Fact] void should_leave_the_slot_unnamed_for_a_structures_own_arrangement() => _wide!.Slot.ShouldBeNull();
    [Fact] void should_carry_no_slot_placement_variant() => _wide!.Slots.ShouldBeNull();
    [Fact] void should_carry_no_element_placement_variant() => _wide!.Elements.ShouldBeNull();
}
