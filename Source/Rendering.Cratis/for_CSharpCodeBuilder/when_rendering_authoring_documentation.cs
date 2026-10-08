// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CSharpCodeBuilder;

public class when_rendering_authoring_documentation : Specification
{
    string _result = null!;

    void Because() => _result = new CSharpCodeBuilder()
        .OpenBlock("public class Example")
        .Documentation("Use <item> & \"name\" 'value' > other  \r\nNext\rLast", "# Notes  \r\n\r\n- `a < b & c > d`\rFinal\r\n  ")
        .Line("public void Run() { }")
        .EndBlock()
        .ToString();

    [Fact] void should_escape_xml_without_reinterpreting_markdown() => _result.ShouldContain("    /// - `a &lt; b &amp; c &gt; d`");
    [Fact] void should_preserve_quotes_as_text() => _result.ShouldContain("    /// Use &lt;item&gt; &amp; \"name\" 'value' &gt; other");
    [Fact] void should_normalize_both_line_ending_forms() => _result.ShouldContain("    /// Next\n    /// Last\n    /// </summary>");
    [Fact] void should_preserve_internal_blank_lines() => _result.ShouldContain("    /// # Notes\n    ///\n    /// - `a &lt; b &amp; c &gt; d`");
    [Fact] void should_trim_trailing_whitespace_and_blank_lines() => _result.ShouldContain("    /// Final\n    /// </remarks>");
    [Fact] void should_keep_the_legacy_summary_bytes_when_metadata_is_absent() => new CSharpCodeBuilder().Documentation(null, fallbackSummary: "Existing summary.").ToString().ShouldEqual(new CSharpCodeBuilder().Summary("Existing summary.").ToString());
    [Fact] void should_emit_nothing_when_metadata_and_fallback_are_absent() => new CSharpCodeBuilder().Documentation(null).ToString().ShouldEqual(new CSharpCodeBuilder().ToString());
}
