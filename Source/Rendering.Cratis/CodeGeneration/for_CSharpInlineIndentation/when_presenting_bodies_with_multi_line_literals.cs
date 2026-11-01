// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpInlineIndentation;

public class when_presenting_bodies_with_multi_line_literals : Specification
{
    [Theory]
    [InlineData("    var s = \"\"\"\n      raw\n        content\n      \"\"\";\n    return s;", "        var s = \"\"\"\n      raw\n        content\n      \"\"\";\n        return s;")]
    [InlineData("    var s = @\"one\n  two\n\";\n    return s;", "        var s = @\"one\n  two\n\";\n        return s;")]
    [InlineData("    var s = $\"\"\"\n      a {1}\n      \"\"\";\n    return s;", "        var s = $\"\"\"\n      a {1}\n      \"\"\";\n        return s;")]
    [InlineData("    var s = $@\"one {1}\n  two\n\";\n    return s;", "        var s = $@\"one {1}\n  two\n\";\n        return s;")]
    void should_reindent_around_but_never_inside_the_literal(string body, string expected) =>
        CSharpInlineIndentation.Present(body, 2, InlineCodeScope.MemberBody).ShouldEqual(expected);
}
