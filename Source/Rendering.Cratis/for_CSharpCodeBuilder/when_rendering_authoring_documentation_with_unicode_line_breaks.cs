// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CSharpCodeBuilder;

public class when_rendering_authoring_documentation_with_unicode_line_breaks : Specification
{
    string _result = null!;

    void Because() => _result = new CSharpCodeBuilder()
        .Documentation("Summary\u0085Next line\u2028Line separator\u2029Paragraph separator", "Remarks\u0085Next line\u2028Line separator\u2029Paragraph separator")
        .Line("public class Example;")
        .ToString();

    [Fact] void should_prefix_text_after_a_next_line_character() => _result.ShouldContain("/// Summary\n/// Next line\n");
    [Fact] void should_prefix_text_after_a_line_separator() => _result.ShouldContain("/// Next line\n/// Line separator\n");
    [Fact] void should_prefix_text_after_a_paragraph_separator() => _result.ShouldContain("/// Line separator\n/// Paragraph separator\n/// </summary>");
    [Fact] void should_normalize_unicode_line_breaks_in_remarks() => _result.ShouldContain("/// Remarks\n/// Next line\n/// Line separator\n/// Paragraph separator\n/// </remarks>");
    [Fact] void should_not_leave_a_next_line_character_in_source() => _result.ShouldNotContain("\u0085");
    [Fact] void should_not_leave_a_line_separator_in_source() => _result.ShouldNotContain("\u2028");
    [Fact] void should_not_leave_a_paragraph_separator_in_source() => _result.ShouldNotContain("\u2029");
}
