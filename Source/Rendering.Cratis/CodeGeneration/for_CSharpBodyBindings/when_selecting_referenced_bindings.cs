// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpBodyBindings;

public class when_selecting_referenced_bindings : Specification
{
    static readonly string[] Candidates = ["Title", "Class", "Amount"];

    [Fact]
    void should_select_only_the_names_the_body_uses() =>
        CSharpBodyBindings.Referenced("return [new Notify(Title)];", Candidates).Single().ShouldEqual("Title");

    [Fact]
    void should_select_nothing_when_the_body_uses_no_binding() =>
        CSharpBodyBindings.Referenced("return null;", Candidates).Count.ShouldEqual(0);

    [Fact]
    void should_not_count_a_member_of_another_object() =>
        CSharpBodyBindings.Referenced("return [new Notify(other.Title)];", Candidates).Count.ShouldEqual(0);

    [Fact]
    void should_count_an_escaped_identifier_by_its_unescaped_name() =>
        CSharpBodyBindings.Referenced("return [new Notify(@Class)];", Candidates).Single().ShouldEqual("Class");

    [Fact]
    void should_not_count_a_named_argument_or_initializer_member() =>
        CSharpBodyBindings.Referenced("return [new Notify(Amount: 1) { Title = \"x\" }];", Candidates).Count.ShouldEqual(0);

    [Fact]
    void should_count_an_assignment_target_inside_an_array_initializer() =>
        CSharpBodyBindings.Referenced("var titles = new[] { Title += \"!\" }; return [new Notify(titles[0])];", Candidates).Single().ShouldEqual("Title");

    [Fact]
    void should_count_a_simple_assignment_target_inside_an_implicit_array_initializer() =>
        CSharpBodyBindings.Referenced("int[] amounts = { Amount = 2 }; return null;", Candidates).Single().ShouldEqual("Amount");

    [Fact]
    void should_not_count_a_with_initializer_member() =>
        CSharpBodyBindings.Referenced("var copy = other with { Title = \"x\" }; return null;", Candidates).Count.ShouldEqual(0);

    [Fact]
    void should_count_a_name_used_inside_an_interpolation() =>
        CSharpBodyBindings.Referenced("var text = $\"{Amount} items\"; return null;", Candidates).Single().ShouldEqual("Amount");

    [Fact]
    void should_not_count_a_name_that_only_appears_in_a_string_or_comment() =>
        CSharpBodyBindings.Referenced("// Title\nvar text = \"Amount\"; return null;", Candidates).Count.ShouldEqual(0);
}
