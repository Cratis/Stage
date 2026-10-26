// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CSharpCodeBuilder;

public class when_preserving_literal_bytes_in_member_bodies : Specification
{
    string _rewritten = null!;
    string _finalized = null!;

    void Because()
    {
        _rewritten = CreateBuilder().ToString();
        _finalized = CreateBuilder().ToString(body => body.Replace("=> null!;", "=> default!;", StringComparison.Ordinal));
    }

    [Fact] void should_preserve_crlf_inside_a_verbatim_literal_during_import_rewriting() => _rewritten.ShouldContain("        var crlf = @\"line1\r\nline2\";\n");
    [Fact] void should_preserve_lf_inside_a_verbatim_literal_during_import_rewriting() => _rewritten.ShouldContain("        var lf = @\"line1\nline2\";\n");
    [Fact] void should_preserve_raw_multiline_literal_indentation_during_import_rewriting() => _rewritten.ShouldContain("        var raw = \"\"\"\n            first\n                global::System.Text.RegularExpressions.Regex // { }\n            last\n        \"\"\";\n");
    [Fact] void should_preserve_escaped_quotes_comment_text_and_braces_during_import_rewriting() => _rewritten.ShouldContain("        var escaped = \"\\\"quoted\\\" // /* */ { global::System.Text.RegularExpressions.Regex }\";\n");
    [Fact] void should_still_rewrite_the_generated_type_outside_the_authored_body() => _rewritten.ShouldContain("    public Regex Pattern() => null!;\n");
    [Fact] void should_preserve_the_complete_authored_body_during_file_finalization() => _finalized.ShouldEqual(
        "namespace Shop.Common;\n\n" +
        "public class Draft\n" +
        "{\n" +
        "    public global::System.Text.RegularExpressions.Regex Pattern() => default!;\n" +
        "    public string[] Handle()\n" +
        "    {\n" +
        "        var crlf = @\"line1\r\nline2\";\n" +
        "        var lf = @\"line1\nline2\";\n" +
        "        var raw = \"\"\"\n" +
        "            first\n" +
        "                global::System.Text.RegularExpressions.Regex // { }\n" +
        "            last\n" +
        "        \"\"\";\n" +
        "        var escaped = \"\\\"quoted\\\" // /* */ { global::System.Text.RegularExpressions.Regex }\";\n" +
        "        return [crlf, lf, raw, escaped];\n" +
        "    }\n" +
        "}\n");

    static CSharpCodeBuilder CreateBuilder() => new CSharpCodeBuilder(new ImportRequest("Shop.Common", GeneratedProjects.Application, [], []))
        .Namespace("Shop.Common")
        .OpenBlock("public class Draft")
        .ExpressionMember("public global::System.Text.RegularExpressions.Regex Pattern()", "null!")
        .OpenBlock("public string[] Handle()")
        .RawVerbatim(
            "        var crlf = @\"line1\r\nline2\";\n" +
            "        var lf = @\"line1\nline2\";\n" +
            "        var raw = \"\"\"\n" +
            "            first\n" +
            "                global::System.Text.RegularExpressions.Regex // { }\n" +
            "            last\n" +
            "        \"\"\";\n" +
            "        var escaped = \"\\\"quoted\\\" // /* */ { global::System.Text.RegularExpressions.Regex }\";\n" +
            "        return [crlf, lf, raw, escaped];\n")
        .EndBlock()
        .EndBlock();
}
