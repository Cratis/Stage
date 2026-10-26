// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpBodyContainment;

public class when_proving_member_boundaries : Specification
{
    [Theory]
    [InlineData("return 1;")]
    [InlineData("if (true) { return 1; } return 2;")]
    [InlineData("// }\nreturn 1;")]
    [InlineData("/* } */ return 1;")]
    [InlineData("return \"} public class Evil {\";")]
    [InlineData("return @\"}\n{\";")]
    [InlineData("return \"\"\"\n}\n{\n\"\"\";")]
    [InlineData("return $$\"\"\"literal } {{1}}\"\"\";")]
    [InlineData("return 'a';")]
    [InlineData("return $\"value {1}\";")]
    void should_prove_balanced_authored_statements_and_literal_braces(string text) => CSharpBodyContainment.Analyze(text).IsContained.ShouldBeTrue();

    [Theory]
    [InlineData("} public class Evil { public void Run() { }")]
    [InlineData("} public void Evil() { } {")]
    [InlineData("return 1; /*")]
    [InlineData("return @\"unterminated")]
    [InlineData("return \"unterminated")]
    [InlineData("return \"\"\"unterminated")]
    [InlineData("return $@\"unterminated")]
    [InlineData("return $$\"\"\"unterminated")]
    [InlineData("return $\"\"\"unterminated")]
    [InlineData("return \"\"\"\"unterminated")]
    [InlineData("return \"\"\"\nunterminated")]
    [InlineData("return $$\"\"\"\nunterminated {{1}}")]
    [InlineData("return $\"unterminated")]
    [InlineData("return @$\"unterminated")]
    [InlineData("return 'a")]
    [InlineData("return '")]
    [InlineData("#if false\nreturn \"\"\"unterminated\n#endif")]
    [InlineData("if (true) { return 1;")]
    [InlineData("#if false\n} public class Evil {}\n#endif")]
    [InlineData("#if false\n/*\n#endif")]
    void should_refuse_escape_and_unprovable_lexical_containment(string text) => CSharpBodyContainment.Analyze(text).IsContained.ShouldBeFalse();

    [Fact]
    void should_distinguish_contained_unsupported_directives_from_integrity_failures()
    {
        var proof = CSharpBodyContainment.Analyze("#if false\nreturn 1;\n#endif\nreturn 2;");
        proof.IsContained.ShouldBeTrue();
        proof.Supported.ShouldBeFalse();
        proof.HasDirectives.ShouldBeTrue();
    }
}
