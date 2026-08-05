// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CSharpCodeBuilder;

public class when_embedding_raw_code : Specification
{
    string _result = null!;

    void Because()
    {
        var builder = new CSharpCodeBuilder()
            .OpenBlock("public void Handle()")
            .Raw("var x = 1;\nif (x > 0)\n    DoSomething();")
            .EndBlock();
        _result = builder.ToString();
    }

    [Fact] void should_indent_every_raw_line_at_the_current_block_level() => _result.ShouldContain("    var x = 1;");
    [Fact] void should_preserve_the_relative_indentation_of_nested_raw_lines() => _result.ShouldContain("        DoSomething();");
}
