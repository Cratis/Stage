// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpInlineIndentation;

public class when_presenting_mixed_leading_whitespace : Specification
{
    [Fact]
    void should_strip_only_the_common_string_prefix() =>
        CSharpInlineIndentation.Present("\t  a;\n\t    b;", 1, InlineCodeScope.MemberBody).ShouldEqual("    a;\n      b;");

    [Fact]
    void should_only_add_the_level_when_no_prefix_is_shared() =>
        CSharpInlineIndentation.Present(" a;\n\tb;", 1, InlineCodeScope.MemberBody).ShouldEqual("     a;\n    \tb;");
}
