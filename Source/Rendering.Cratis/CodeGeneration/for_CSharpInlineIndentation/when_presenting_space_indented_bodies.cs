// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpInlineIndentation;

public class when_presenting_space_indented_bodies : Specification
{
    [Fact]
    void should_strip_the_common_prefix_and_apply_the_level() =>
        CSharpInlineIndentation.Present("        if (x)\n        {\n            return y;\n        }", 2, InlineCodeScope.MemberBody)
            .ShouldEqual("        if (x)\n        {\n            return y;\n        }");

    [Fact]
    void should_reindent_to_a_deeper_level() =>
        CSharpInlineIndentation.Present("  if (x)\n  {\n    return y;\n  }", 3, InlineCodeScope.MemberBody)
            .ShouldEqual("            if (x)\n            {\n              return y;\n            }");

    [Fact]
    void should_keep_the_line_count() =>
        CSharpInlineIndentation.Lines(CSharpInlineIndentation.Present("    a;\n    b;\n    c;\n", 1, InlineCodeScope.MemberBody)).Count.ShouldEqual(4);
}
