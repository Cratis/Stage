// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpInlineIndentation;

public class when_presentation_cannot_be_verified : Specification
{
    [Fact]
    void should_fall_back_to_the_verbatim_text_without_refusing() =>
        CSharpInlineIndentation.Present("  a;\n  b;", 1, InlineCodeScope.MemberBody, (text, _) => text.Replace("a;", "c;", StringComparison.Ordinal)).ShouldEqual("  a;\n  b;");

    [Fact]
    void should_fall_back_when_a_protected_line_changed() =>
        CSharpInlineIndentation.Verify("var s = @\"a\n  b\";", "var s = @\"a\n b\";", InlineCodeScope.MemberBody).ShouldBeFalse();

    [Fact]
    void should_fall_back_when_a_line_break_changed() =>
        CSharpInlineIndentation.Verify("a;\r\nb;", "a;\nb;", InlineCodeScope.MemberBody).ShouldBeFalse();

    [Fact]
    void should_accept_a_faithful_presentation() =>
        CSharpInlineIndentation.Verify("a;\nb;", "    a;\n    b;", InlineCodeScope.MemberBody).ShouldBeTrue();

    [Fact]
    void should_verify_containment_on_the_presented_text() =>
        CSharpInlineIndentation.Verify("a;\nb;", "a;\nb; /*", InlineCodeScope.MemberBody).ShouldBeFalse();

    [Fact]
    void should_hold_containment_on_the_reindented_text() =>
        CSharpBodyContainment.Analyze(CSharpInlineIndentation.Present("    if (x)\n    {\n        return \"}\";\n    }", 3, InlineCodeScope.MemberBody)).IsContained.ShouldBeTrue();
}
