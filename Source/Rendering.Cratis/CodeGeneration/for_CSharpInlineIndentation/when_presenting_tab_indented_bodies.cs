// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpInlineIndentation;

public class when_presenting_tab_indented_bodies : Specification
{
    [Fact]
    void should_strip_the_common_tab_and_keep_the_residual_tab() =>
        CSharpInlineIndentation.Present("\tif (x)\n\t\treturn y;", 1, InlineCodeScope.MemberBody).ShouldEqual("    if (x)\n    \treturn y;");
}
