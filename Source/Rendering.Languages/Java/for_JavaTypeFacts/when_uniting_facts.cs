// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaTypeFacts;

public class when_uniting_facts : Specification
{
    JavaTypeFacts _left = null!;
    JavaTypeFacts _right = null!;
    JavaTypeFacts _union = null!;

    void Because()
    {
        _left = JavaTypeFacts.Parse("{\"types\":{\"example.Outer\":[\"Left\"],\"example.Empty\":[]}}");
        _right = JavaTypeFacts.Parse("{\"types\":{\"example.Outer\":[\"Right\",\"Left\"],\"example.Other\":[]}}");
        _union = _left.Union(_right);
    }

    [Fact] void should_union_names_per_type() => (_union.TryGet("example.Outer", out var names) && names.SetEquals(["Left", "Right"])).ShouldBeTrue();
    [Fact] void should_keep_known_empty_entries_on_both_sides() => (_union.TryGet("example.Empty", out _) && _union.TryGet("example.Other", out _)).ShouldBeTrue();
    [Fact] void should_not_mutate_the_left_operand() => (_left.TryGet("example.Outer", out var names) && names.SetEquals(["Left"])).ShouldBeTrue();
    [Fact] void should_not_mutate_the_right_operand() => (_right.TryGet("example.Outer", out var names) && names.SetEquals(["Left", "Right"])).ShouldBeTrue();
}
