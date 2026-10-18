// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Scene.Model.Layouts;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ArrangementConverter.when_converting_to_syntax;

public class and_a_flow_arrangement_has_a_row_root : Specification
{
    FlowRow _row = null!;
    FlowArrangement _converted = null!;

    void Establish() => _row = new FlowRow { Gap = 4, Children = [new FlowSlotLeaf("left"), new FlowSlotLeaf("right") { Grow = 1 }] };

    void Because() => _converted = (FlowArrangement)ArrangementConverter.Convert(ArrangementConverter.ToSyntax(new FlowArrangement(_row, []))!)!;

    [Fact] void should_read_the_row_back_inside_the_plain_column_the_text_wraps_a_root_in() => _converted.Root.ShouldBeOfExactType<FlowColumn>();
    [Fact] void should_keep_the_row_as_the_only_child() => JsonSerializer.Serialize(((FlowColumn)_converted.Root).Children[0], typeof(FlowNode)).ShouldEqual(JsonSerializer.Serialize(_row, typeof(FlowNode)));
}
