// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Layouts;
using Cratis.Scene.Model.SizeClasses;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_LayoutConverter.when_converting;

public class and_the_layout_uses_freeform_arrangement : Specification
{
    LayoutSyntax _syntax = null!;
    Layout _result = null!;

    void Establish()
    {
        var variant = new VariantSyntax(
            "compact",
            "regular",
            [
                new PlaceSyntax("header", SourceLocation.Start, Hidden: false, X: 0, Y: 0, SizeWidth: "320", SizeHeight: "48"),
                new PlaceSyntax("footer", SourceLocation.Start, Hidden: true),
            ],
            SourceLocation.Start);

        _syntax = new(
            "Storyboard",
            [new SlotSyntax("header", null, SourceLocation.Start), new SlotSyntax("footer", null, SourceLocation.Start)],
            SourceLocation.Start,
            new ArrangementSyntax(ArrangementMode.Freeform, SourceLocation.Start, null, null, [variant]));
    }

    void Because() => _result = LayoutConverter.Convert(_syntax);

    [Fact] void should_use_a_freeform_arrangement() => _result.Arrangement.ShouldBeOfExactType<FreeformSlotArrangement>();

    [Fact]
    void should_have_one_variant_for_the_declared_size_class()
    {
        var freeform = (FreeformSlotArrangement)_result.Arrangement!;
        freeform.Variants.Count.ShouldEqual(1);
        freeform.Variants[0].SizeClass.ShouldEqual(new SizeClass(WidthSizeClass.Compact, HeightSizeClass.Regular));
    }

    [Fact]
    void should_exclude_the_hidden_place_from_the_variants_placements()
    {
        var placements = ((FreeformSlotArrangement)_result.Arrangement!).Variants[0].Placements;
        placements.Count.ShouldEqual(1);
        placements[0].SlotName.ShouldEqual("header");
    }
}
