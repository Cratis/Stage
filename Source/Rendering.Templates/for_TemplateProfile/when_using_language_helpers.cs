// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplateProfile;

public class when_using_language_helpers : for_TemplatePreparer.given.a_kit
{
    [Fact]
    public void should_use_invariant_word_arrays_and_target_owned_literals()
    {
        using var plan = TemplateProfile.Prepare(Kit("{{ name.pascal model.words }}|{{ name.camel model.words }}|{{ name.snake model.words }}|{{ lit.identifier 'class' }}|{{ lit.string model.text }}"), new given.a_language()).Templates!.BeginPlan(TemplateBudget.Default);
        using var json = JsonDocument.Parse("""{"model":{"words":["order","iD","eMail"],"text":"quote\""}}""");
        var result = plan.Render("main.scriban", json.RootElement);
        result.Refusal.ShouldBeNull();
        ((TemplateTextSegment)result.Output!.Segments[0]).Text.ShouldEqual("OrderIDEMail|orderIDEMail|order_id_email|@class|\"quote\\u0022\"");
    }

    [Theory]
    [InlineData("\"raw text\"")]
    [InlineData("[]")]
    [InlineData("[\"\"]")]
    [InlineData("[1]")]
    public void should_reject_nonword_inputs(string input)
    {
        using var plan = TemplateProfile.Prepare(Kit("{{ name.pascal model.words }}"), new given.a_language()).Templates!.BeginPlan(TemplateBudget.Default);
        using var json = JsonDocument.Parse("{\"model\":{\"words\":" + input + "}}");
        plan.Render("main.scriban", json.RootElement).Refusal!.Code.ShouldEqual(TemplateCodes.Input);
    }

    [Theory]
    [InlineData("{{ lit.integer '42' }}")]
    [InlineData("{{ lit.boolean 'true' }}")]
    [InlineData("{{ lit.string 42 }}")]
    public void should_reject_literal_coercions(string template)
    {
        using var plan = TemplateProfile.Prepare(Kit(template), new given.a_language()).Templates!.BeginPlan(TemplateBudget.Default);
        plan.Render("main.scriban", JsonSerializer.SerializeToElement(new { model = new { } })).Refusal!.Code.ShouldEqual(TemplateCodes.Input);
    }

    [Fact]
    public void should_preserve_symbol_types_and_wrapper_order()
    {
        using var plan = TemplateProfile.Prepare(Kit("{{ type.ref model.type }}"), new given.a_language()).Templates!.BeginPlan(TemplateBudget.Default);
        using var json = JsonDocument.Parse("""{"model":{"type":{"kind":"primitive","primitive":"uuid","isCollection":true,"isOptional":true}}}""");
        var result = plan.Render("main.scriban", json.RootElement);
        result.Refusal.ShouldBeNull();
        result.Output!.Segments[0].ShouldEqual(new TemplateSymbolSegment(TemplateSymbolKind.Reference, new("System", "Guid")));
        ((TemplateTextSegment)result.Output.Segments[1]).Text.ShouldEqual("[]?");
    }
}
