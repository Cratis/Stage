// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplatePlanSession;

public class when_emitting_symbols : for_TemplatePreparer.given.a_kit
{
    static JsonElement Scope => JsonSerializer.SerializeToElement(new { model = new { symbol = new Dictionary<string, object> { ["$symbol"] = true, ["identity"] = "domain.Customer", ["namespace"] = "domain", ["name"] = "Customer" } } });

    [Fact]
    public void should_emit_structured_symbol_segments()
    {
        using var session = TemplatePreparer.Prepare(Kit("A{{ sym.ref model.symbol }}B")).Value!.BeginPlan(TemplateBudget.Default);
        var result = session.Render("main.scriban", Scope);
        result.Refusal.ShouldBeNull();
        var output = result.Output!;
        output.Segments.Length.ShouldEqual(3);
        ((TemplateTextSegment)output.Segments[0]).Text.ShouldEqual("A");
        ((TemplateSymbolUseSegment)output.Segments[1]).Symbol.Identity.ShouldEqual("domain.Customer");
        ((TemplateTextSegment)output.Segments[2]).Text.ShouldEqual("B");
    }

    [Theory]
    [InlineData("{{ capture x; sym.ref model.symbol; end }}")]
    [InlineData("{{ (sym.ref model.symbol) + 'text' }}")]
    [InlineData("{{ (sym.ref model.symbol) == 'text' }}")]
    [InlineData("{{ (sym.ref model.symbol) | string.upcase }}")]
    [InlineData("{{ x = [sym.ref model.symbol]; x }}")]
    [InlineData("{{ x = sym.ref model.symbol; if x; 'x'; end }}")]
    public void should_refuse_text_and_container_coercion(string text)
    {
        using var session = TemplatePreparer.Prepare(Kit(text)).Value!.BeginPlan(TemplateBudget.Default);
        var result = session.Render("main.scriban", Scope);
        result.Refusal!.Code.ShouldEqual(TemplateCodes.Symbol);
        result.Output.ShouldBeNull();
    }

    [Fact]
    public void should_not_interpret_similar_plain_json_as_a_symbol()
    {
        using var session = TemplatePreparer.Prepare(Kit("{{ sym.ref model.symbol }}")).Value!.BeginPlan(TemplateBudget.Default);
        var scope = JsonSerializer.SerializeToElement(new { model = new { symbol = new { identity = "domain.Customer" } } });
        session.Render("main.scriban", scope).Refusal!.Code.ShouldEqual(TemplateCodes.Input);
    }
}
