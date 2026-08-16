// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Layouts;
using Cratis.Scene.Model.SizeClasses;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ArrangementSelector.when_selecting;

public class and_a_freeform_arrangement_targets_another_size_class : Specification
{
    FreeformSlotArrangement _arrangement = null!;
    ArrangementSelection? _targeted;
    ArrangementSelection? _untargeted;

    void Establish() => _arrangement = new FreeformSlotArrangement(
    [
        new FreeformSlotVariant(
            new SizeClass(WidthSizeClass.Regular, HeightSizeClass.Regular),
            [new SlotPlacement("navigation", 0, 0, 240, 1024)]),
    ]);

    void Because()
    {
        _targeted = ArrangementSelector.Select("AppShell", "content", _arrangement, new SizeClass(WidthSizeClass.Regular, HeightSizeClass.Regular));
        _untargeted = ArrangementSelector.Select("AppShell", "content", _arrangement, new SizeClass(WidthSizeClass.Compact, HeightSizeClass.Regular));
    }

    [Fact] void should_select_the_variant_that_targets_the_size_class() => _targeted!.Slots!.Placements.Single().SlotName.ShouldEqual("navigation");
    [Fact] void should_name_the_slot_the_arrangement_belongs_to() => _targeted!.Slot.ShouldEqual("content");
    [Fact] void should_select_nothing_rather_than_fall_back_to_another_variant() => _untargeted.ShouldBeNull();
}
