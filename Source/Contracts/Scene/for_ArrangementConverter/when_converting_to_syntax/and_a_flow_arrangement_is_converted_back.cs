// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Scene.Model.Layouts;
using Cratis.Scene.Model.SizeClasses;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ArrangementConverter.when_converting_to_syntax;

public class and_a_flow_arrangement_is_converted_back : Specification
{
    FlowArrangement _arrangement = null!;
    ArrangementSyntax _syntax = null!;
    FlowArrangement _converted = null!;

    void Establish() => _arrangement = new(
        new FlowColumn
        {
            Gap = 8,
            Children =
            [
                new FlowSlotLeaf("topbar"),
                new FlowRow { Gap = 4, Grow = 1, Children = [new FlowSlotLeaf("sidebar") { Span = 2 }, new FlowSlotLeaf("content") { Grow = 3 }] },
                new FlowGrid { Columns = 2, Rows = 3, Children = [new FlowSlotLeaf("footer")] }
            ]
        },
        [new FlowOverride(WidthSizeClass.Compact, null, new FlowColumn { Children = [new FlowSlotLeaf("content")] })]);

    void Because()
    {
        _syntax = ArrangementConverter.ToSyntax(_arrangement)!;
        _converted = (FlowArrangement)ArrangementConverter.Convert(_syntax)!;
    }

    [Fact] void should_be_a_flow_arrangement() => _syntax.Mode.ShouldEqual(ArrangementMode.Flow);
    [Fact] void should_convert_back_to_the_same_root() => Json(_converted.Root).ShouldEqual(Json(_arrangement.Root));
    [Fact] void should_keep_the_override_for_the_width_class() => _converted.Overrides.Count.ShouldEqual(1);
    [Fact] void should_keep_the_override_width() => _converted.Overrides[0].Width.ShouldEqual(WidthSizeClass.Compact);
    [Fact] void should_keep_the_override_root() => Json(_converted.Overrides[0].Root).ShouldEqual(Json(_arrangement.Overrides[0].Root));

    static string Json(FlowNode node) => JsonSerializer.Serialize(node, node.GetType());
}
