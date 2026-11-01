// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpInlineIndentation;

public class when_presenting_crlf_bodies : Specification
{
    [Fact]
    void should_preserve_every_line_break() =>
        CSharpInlineIndentation.Present("  a;\r\n  b;\r  c;\n  d;", 1, InlineCodeScope.MemberBody).ShouldEqual("    a;\r\n    b;\r    c;\n    d;");

    [Fact]
    void should_not_introduce_mixed_breaks() =>
        CSharpInlineIndentation.Present("  a;\r\n  b;\r\n", 2, InlineCodeScope.MemberBody).Contains("\r\n").ShouldBeTrue();
}
