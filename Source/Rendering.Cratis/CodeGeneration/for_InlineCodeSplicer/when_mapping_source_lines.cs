// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;
using static Cratis.Stage.Rendering.Cratis.CodeGeneration.for_InlineCode.when_verifying_original_bytes;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_InlineCodeSplicer;

public class when_mapping_source_lines : Specification
{
    static InlineCodeSplice Splice(string before, string body, InlineSourceLine? source, string after = "}\n")
    {
        var registry = new InlineCodeTokens();
        var token = registry.Issue(Code(body), Site, new(Source: source)).Token;
        return InlineCodeSplicer.Splice($"{before}{token}{after}", registry);
    }

    [Fact]
    void should_wrap_a_token_that_starts_a_line()
    {
        var result = Splice("{\n", "return 1;", new(7, 1, "../a.play"));
        result.Text.ShouldEqual("{\n#line 7 \"../a.play\"\nreturn 1;\n#line default\n}\n");
    }

    [Fact]
    void should_keep_the_authored_span_on_the_presentation_only()
    {
        var result = Splice("{\n", "return 1;", new(7, 1, "a.play"));
        var span = Assert.Single(result.Authored);
        result.Text.Substring(span.Start, span.Length).ShouldEqual("return 1;\n");
    }

    [Fact]
    void should_wrap_a_multi_line_body_with_its_first_line()
    {
        var result = Splice("{\n", "var a = 1;\nreturn a;", new(12, 2, "a.play"));
        result.Text.ShouldEqual("{\n#line 12 \"a.play\"\nvar a = 1;\nreturn a;\n#line default\n}\n");
    }

    [Fact]
    void should_not_wrap_without_a_source()
    {
        var result = Splice("{\n", "return 1;", null);
        result.Text.ShouldEqual("{\nreturn 1;\n}\n");
    }

    [Fact]
    void should_not_wrap_a_token_in_the_middle_of_a_line()
    {
        var result = Splice("{ ", "return 1;", new(7, 1, "a.play"));
        result.Text.ShouldEqual("{ return 1;\n}\n");
    }

    [Fact]
    void should_not_wrap_when_the_line_count_differs()
    {
        var result = Splice("{\n", "return 1;", new(7, 2, "a.play"));
        result.Text.ShouldEqual("{\nreturn 1;\n}\n");
    }
}
