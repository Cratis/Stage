// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Layouts;
using Cratis.Scene.Model.Screens;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenTemplateConverter.when_converting;

public class and_the_template_fits_a_slot : Specification
{
    ScreenTemplateSyntax _syntax = null!;
    ScreenTemplate _result = null!;

    void Establish()
    {
        var root = new ArrangementContainerSyntax(
            ArrangementContainerKind.Row,
            [
                new ArrangementSlotSyntax("sidebar", SourceLocation.Start, Width: 280, Height: null, Grow: false, Span: null),
                new ArrangementSlotSyntax("main", SourceLocation.Start, Width: null, Height: null, Grow: true, Span: null),
            ],
            SourceLocation.Start,
            Gap: 16);

        var stackedOverride = new ArrangementOverrideSyntax(
            "compact",
            null,
            new ArrangementContainerSyntax(
                ArrangementContainerKind.Column,
                [
                    new ArrangementSlotSyntax("main", SourceLocation.Start, null, null, false, null),
                    new ArrangementSlotSyntax("sidebar", SourceLocation.Start, null, null, false, null),
                ],
                SourceLocation.Start,
                null),
            SourceLocation.Start);

        _syntax = new(
            "MasterDetail",
            [new SlotSyntax("sidebar", null, SourceLocation.Start), new SlotSyntax("main", null, SourceLocation.Start)],
            SourceLocation.Start,
            FitsSlot: "content",
            new ArrangementSyntax(ArrangementMode.Flow, SourceLocation.Start, root, [stackedOverride], null));
    }

    void Because() => _result = ScreenTemplateConverter.Convert(_syntax);

    [Fact] void should_carry_the_template_name() => _result.Name.ShouldEqual("MasterDetail");
    [Fact] void should_carry_the_slot_it_fits_into() => _result.FitsSlot.ShouldEqual("content");
    [Fact] void should_carry_the_slots_it_offers() => _result.Slots.Select(slot => slot.Name).ShouldEqual(["sidebar", "main"]);
    [Fact] void should_use_a_flow_arrangement() => _result.Arrangement.ShouldBeOfExactType<FlowArrangement>();
    [Fact] void should_carry_the_size_class_override() => ((FlowArrangement)_result.Arrangement!).Overrides.Count.ShouldEqual(1);
    [Fact] void should_carry_the_overrides_width_size_class() => ((FlowArrangement)_result.Arrangement!).Overrides[0].Width.ShouldEqual(Cratis.Scene.Model.SizeClasses.WidthSizeClass.Compact);
    [Fact] void should_leave_the_overrides_height_size_class_unset() => ((FlowArrangement)_result.Arrangement!).Overrides[0].Height.ShouldBeNull();
    [Fact] void should_provide_no_content_of_its_own() => _result.Content.ShouldBeNull();
}
