// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpCodeBuilder;

public class when_mapping_authored_bodies_to_source_lines
{
    static readonly Dictionary<string, InlineSourceLine> _lines = new() { ["req-1"] = new(30, 2, "../Model/a.play") };

    static string Render(CSharpCodeBuilder builder, string requirement) =>
        builder.Namespace("Shop.Common")
            .OpenBlock("public static class Bodies")
            .OpenBlock("public static int Run()")
            .RawVerbatim("var a = 1;\nreturn a;", requirement)
            .EndBlock()
            .EndBlock()
            .ToString();

    [Fact]
    void should_wrap_the_re_indented_body_in_line_directives_on_their_own_lines()
    {
        var text = Render(new() { SourceLines = _lines }, "req-1");
        Assert.Contains("{\n#line 30 \"../Model/a.play\"\n        var a = 1;\n        return a;\n#line default\n    }", text.ReplaceLineEndings("\n"));
    }

    [Fact]
    void should_not_wrap_a_requirement_without_a_mapping()
    {
        Assert.DoesNotContain("#line", Render(new() { SourceLines = _lines }, "other"));
    }

    [Fact]
    void should_not_wrap_a_builder_without_mappings()
    {
        Assert.DoesNotContain("#line", Render(new(), "req-1"));
    }

    [Fact]
    void should_keep_directives_through_import_rewriting()
    {
        var builder = new CSharpCodeBuilder(new ImportRequest("Shop.Common", GeneratedProjects.Application, [], [])) { SourceLines = _lines };
        var text = Render(builder, "req-1").ReplaceLineEndings("\n");
        Assert.Contains("#line 30 \"../Model/a.play\"\n", text);
        Assert.Contains("#line default\n", text);
    }
}
