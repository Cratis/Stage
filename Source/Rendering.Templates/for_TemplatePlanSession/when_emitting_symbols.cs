// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplatePlanSession;

public class when_emitting_symbols : for_TemplatePreparer.given.a_kit
{
    static JsonElement Scope => JsonSerializer.SerializeToElement(new { model = new { name = "Customer" } });

    [Fact]
    public void should_emit_declarations_references_and_imports_in_order()
    {
        using var session = TemplatePreparer.Prepare(Kit("A{{ sym.decl 'Domain' model.name }}{{ sym.ref 'Other' model.name }}{{ sym.imports }}B")).Value!.BeginPlan(TemplateBudget.Default);
        var result = session.Render("main.scriban", Scope);
        result.Refusal.ShouldBeNull();
        var output = result.Output!;
        output.Segments.Length.ShouldEqual(5);
        ((TemplateSymbolSegment)output.Segments[1]).ShouldEqual(new(TemplateSymbolKind.Declaration, new("Domain", "Customer")));
        ((TemplateSymbolSegment)output.Segments[2]).ShouldEqual(new(TemplateSymbolKind.Reference, new("Other", "Customer")));
        ((TemplateSymbolSegment)output.Segments[3]).ShouldEqual(new(TemplateSymbolKind.Imports, null));
    }

    [Theory]
    [InlineData("{{ capture x; sym.ref 'Domain' 'Customer'; end }}")]
    [InlineData("{{ (sym.ref 'Domain' 'Customer') + 'text' }}")]
    [InlineData("{{ (sym.ref 'Domain' 'Customer') == 'text' }}")]
    [InlineData("{{ (sym.ref 'Domain' 'Customer') | string.upcase }}")]
    [InlineData("{{ x = [sym.ref 'Domain' 'Customer']; x }}")]
    [InlineData("{{ x = sym.ref 'Domain' 'Customer'; x.name }}")]
    public void should_refuse_text_and_container_coercion(string text)
    {
        using var session = TemplatePreparer.Prepare(Kit(text)).Value!.BeginPlan(TemplateBudget.Default);
        var result = session.Render("main.scriban", Scope);
        result.Refusal!.Code.ShouldEqual(TemplateCodes.Symbol);
        result.Output.ShouldBeNull();
    }

    [Fact]
    public void should_allow_opaque_assignment_and_null_test()
    {
        using var session = TemplatePreparer.Prepare(Kit("{{ x = sym.ref 'Domain' 'Customer'; if x != null; x; end }}")).Value!.BeginPlan(TemplateBudget.Default);
        session.Render("main.scriban", Scope).Output!.Segments[0].ShouldEqual(new TemplateSymbolSegment(TemplateSymbolKind.Reference, new("Domain", "Customer")));
    }
}
