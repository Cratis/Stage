// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpInlineIndentation;

public class when_locating_a_presented_body : Specification
{
    const string Body = "  if (x)\n  {\n    return @\"a\n  b\";\n\n  }";

    [Fact]
    void should_find_the_body_at_its_level_and_map_offsets_back()
    {
        var presented = CSharpInlineIndentation.Present(Body, 3, InlineCodeScope.MemberBody);
        var code = "class A\n{\n    void M()\n    {\n" + presented + "\n    }\n}\n";
        var location = CSharpInlineIndentation.Locate(code, Body)!;
        location.Start.ShouldEqual(code.IndexOf("            if (x)", StringComparison.Ordinal));

        // A code line: the `return` keyword.
        location.ToBody(presented.IndexOf("return", StringComparison.Ordinal)).ShouldEqual(Body.IndexOf("return", StringComparison.Ordinal));

        // A protected line: the second line of the literal is unchanged.
        location.ToBody(presented.IndexOf("  b\"", StringComparison.Ordinal) + 2).ShouldEqual(Body.IndexOf("  b\"", StringComparison.Ordinal) + 2);

        // A blank line maps to its start.
        location.ToBody(presented.IndexOf("\n\n", StringComparison.Ordinal) + 1).ShouldEqual(Body.IndexOf("\n\n", StringComparison.Ordinal) + 1);
    }

    [Fact]
    void should_find_a_verbatim_fallback_body()
    {
        var location = CSharpInlineIndentation.Locate("x\n" + Body + "\n", Body)!;
        location.Start.ShouldEqual(2);
        location.ToBody(5).ShouldEqual(5);
    }

    [Fact]
    void should_pick_the_occurrence_at_or_after_the_start_offset()
    {
        var one = CSharpInlineIndentation.Present("a;\nb;", 2, InlineCodeScope.MemberBody);
        var code = one + "\nmid\n" + one + "\n";
        var second = CSharpInlineIndentation.Locate(code, "a;\nb;", code.IndexOf("mid", StringComparison.Ordinal))!;
        second.Start.ShouldEqual(code.LastIndexOf(one, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(64)]
    [InlineData(256)]
    void should_find_bodies_presented_at_any_supported_level(int level)
    {
        var presented = CSharpInlineIndentation.Present("a;\nb;", level, InlineCodeScope.MemberBody);
        var code = "x\n" + presented + "\n";
        var location = CSharpInlineIndentation.Locate(code, "a;\nb;")!;
        location.Start.ShouldEqual(2);
        location.ToBody(presented.IndexOf("b;", StringComparison.Ordinal)).ShouldEqual(3);
    }

    [Fact]
    void should_return_nothing_when_the_body_is_absent() =>
        CSharpInlineIndentation.Locate("class A {}", Body).ShouldBeNull();
}
