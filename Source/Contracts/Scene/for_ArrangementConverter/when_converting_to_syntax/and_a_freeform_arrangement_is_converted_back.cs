// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Layouts;
using Cratis.Scene.Model.SizeClasses;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ArrangementConverter.when_converting_to_syntax;

public class and_a_freeform_arrangement_is_converted_back : Specification
{
    FreeformSlotArrangement _arrangement = null!;
    ArrangementSyntax _syntax = null!;
    FreeformSlotArrangement _converted = null!;

    void Establish() => _arrangement = new([
        new FreeformSlotVariant(new SizeClass(WidthSizeClass.Compact, HeightSizeClass.Regular), [new SlotPlacement("header", 0, 0, 320, 48), new SlotPlacement("body", 0, 48, 320, 0)])
    ]);

    void Because()
    {
        _syntax = ArrangementConverter.ToSyntax(_arrangement)!;
        _converted = (FreeformSlotArrangement)ArrangementConverter.Convert(_syntax)!;
    }

    [Fact] void should_be_a_freeform_arrangement() => _syntax.Mode.ShouldEqual(ArrangementMode.Freeform);
    [Fact] void should_keep_the_variant_size_class() => _converted.Variants[0].SizeClass.ShouldEqual(_arrangement.Variants[0].SizeClass);
    [Fact] void should_keep_every_placement() => _converted.Variants[0].Placements.ShouldEqual(_arrangement.Variants[0].Placements);
}
