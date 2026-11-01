// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplatePreparer;

public class when_scanning_structural_member_names : given.a_kit
{
    [Fact]
    public void should_refuse_33_nested_conditions_using_model_end()
    {
        var text = string.Concat(Enumerable.Repeat("{{ if model.end }}", 33)) + "ok" + string.Concat(Enumerable.Repeat("{{ end }}", 33));
        var result = TemplatePreparer.Prepare(Kit(text));
        result.Diagnostics.Length.ShouldEqual(1);
        result.Diagnostics[0].Code.ShouldEqual(TemplateCodes.Nesting);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData(";")]
    [InlineData("; # comment\n")]
    public void should_count_structural_tokens_at_statement_boundaries(string separator)
    {
        var text = "{{ " + string.Concat(Enumerable.Repeat("if model.end" + separator, 33)) + string.Concat(Enumerable.Repeat("end" + separator, 33)) + " }}";
        var result = TemplatePreparer.Prepare(Kit(text));
        result.Diagnostics.Length.ShouldEqual(1);
        result.Diagnostics[0].Code.ShouldEqual(TemplateCodes.Nesting);
    }

    [Theory]
    [InlineData("end")]
    [InlineData("if")]
    [InlineData("for")]
    public void should_not_count_member_names_as_openers_or_closers(string member)
    {
        var text = string.Concat(Enumerable.Repeat("{{ if model." + member + " }}", 32)) + "ok" + string.Concat(Enumerable.Repeat("{{ end }}", 32));
        using var plan = TemplateProfile.Prepare(Kit(text), new for_TemplateProfile.given.a_language()).Templates.BeginPlan(TemplateBudget.Default);
        var result = plan.Render("main.scriban", JsonSerializer.SerializeToElement(new { model = new Dictionary<string, bool> { [member] = true } }));
        result.Refusal.ShouldBeNull();
        result.Output.ShouldNotBeNull();
        string.Concat(result.Output.Segments.OfType<TemplateTextSegment>().Select(segment => segment.Text)).ShouldEqual("ok");
    }

    [Fact]
    public void should_render_shallow_model_end_usage()
    {
        using var plan = TemplateProfile.Prepare(Kit("{{ if model.end }}{{ model.end }}{{ end }}"), new for_TemplateProfile.given.a_language()).Templates.BeginPlan(TemplateBudget.Default);
        var result = plan.Render("main.scriban", JsonSerializer.SerializeToElement(new { model = new { end = "ok" } }));
        result.Refusal.ShouldBeNull();
        result.Output.ShouldNotBeNull();
        string.Concat(result.Output.Segments.OfType<TemplateTextSegment>().Select(segment => segment.Text)).ShouldEqual("ok");
    }
}
