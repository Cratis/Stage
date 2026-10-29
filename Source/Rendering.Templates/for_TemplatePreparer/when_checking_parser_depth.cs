// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplatePreparer;

public class when_checking_parser_depth : given.a_kit
{
    [Fact]
    public void should_render_a_depth_one_chain_of_40_else_if_branches() => Render(Chain(40)).ShouldEqual("ok");

    [Fact]
    public void should_refuse_an_else_if_chain_that_hits_the_parser_backstop()
    {
        var result = TemplatePreparer.Prepare(Kit(Chain(100)));
        result.Diagnostics.Length.ShouldEqual(1);
        result.Diagnostics[0].Code.ShouldEqual(TemplateCodes.Nesting);
        result.Diagnostics[0].Message.ShouldContain("depth limit `64`");
    }

    [Fact]
    public void should_render_exactly_32_nested_blocks() => Render(Nested(32)).ShouldEqual("ok");

    [Fact]
    public void should_refuse_33_nested_blocks()
    {
        var result = TemplatePreparer.Prepare(Kit(Nested(33)));
        result.Diagnostics.Length.ShouldEqual(1);
        result.Diagnostics[0].Code.ShouldEqual(TemplateCodes.Nesting);
    }

    [Fact]
    public void should_map_nesting_past_the_parser_limit_to_a_nesting_refusal()
    {
        var result = TemplatePreparer.Prepare(Kit(Nested(65)));
        result.Diagnostics.Length.ShouldEqual(1);
        result.Diagnostics[0].Code.ShouldEqual(TemplateCodes.Nesting);
        result.Diagnostics[0].Message.ShouldContain("depth limit `64`");
    }

    [Fact]
    public void should_normalize_else_if_branches_at_the_semantic_depth_limit()
    {
        var text = "{{ if false }}" + string.Concat(Enumerable.Repeat("{{ else if false }}", 20)) + "{{ else }}" + Nested(31) + "{{ end }}";
        Render(text).ShouldEqual("ok");
    }

    [Fact]
    public void should_not_accumulate_depth_across_sequential_newline_blocks()
    {
        var text = "{{ " + string.Concat(Enumerable.Repeat("if true\n end\n", 100)) + " }}ok";
        Render(text).ShouldEqual("ok");
    }

    static string Chain(int count) => "{{ if false }}" + string.Concat(Enumerable.Repeat("{{ else if false }}", count)) + "{{ else }}ok{{ end }}";

    static string Nested(int count) => string.Concat(Enumerable.Repeat("{{ if true }}", count)) + "ok" + string.Concat(Enumerable.Repeat("{{ end }}", count));

    string Render(string text)
    {
        var preparation = TemplateProfile.Prepare(Kit(text), new for_TemplateProfile.given.a_language());
        preparation.Diagnostics.Length.ShouldEqual(0);
        using var plan = preparation.Templates.BeginPlan(TemplateBudget.Default);
        var result = plan.Render("main.scriban", JsonSerializer.SerializeToElement(new { model = new { } }));
        result.Refusal.ShouldBeNull();
        result.Output.ShouldNotBeNull();
        return string.Concat(result.Output.Segments.OfType<TemplateTextSegment>().Select(segment => segment.Text));
    }
}
