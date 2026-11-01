// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpInlineIndentation;

public class when_presenting_bodies_with_blank_lines : Specification
{
    [Fact]
    void should_empty_whitespace_only_lines_and_ignore_them_for_the_prefix() =>
        CSharpInlineIndentation.Present("    a;\n   \n\t\n    b;\n", 1, InlineCodeScope.MemberBody).ShouldEqual("    a;\n\n\n    b;\n");
}
