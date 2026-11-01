// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplateProfile;

public class when_checking_contract_edges : for_TemplatePreparer.given.a_kit
{
    [Theory]
    [InlineData("{{ expr.lower model.x 'operators' 'missing' 'throwing_operation_body' }}")]
    [InlineData("{{ expr.lower model.x 'operators' 'leaf' 'invented' }}")]
    [InlineData("{{ expr.supported model.x model.table }}")]
    public void should_statically_reject_expression_contract_misuse(string text) => TemplateProfile.Prepare(when_lowering.WithOperators(Kit(text)), new given.a_language()).Diagnostics[0].Code.ShouldEqual(TemplateCodes.Helper);

    [Theory]
    [InlineData("{\"open\":\"(\",\"close\":\")\",\"operators\":{\"invented\":{\"text\":\"x\",\"precedence\":1}}}")]
    [InlineData("{\"open\":\"(\",\"close\":\")\",\"operators\":{\"and\":{\"text\":\"x\",\"precedence\":0}}}")]
    [InlineData("{\"open\":\"(\",\"close\":\")\",\"operators\":{\"and\":{\"text\":\"x\",\"precedence\":101}}}")]
    [InlineData("{\"open\":\"(\",\"close\":\")\",\"operators\":{\"and\":{\"text\":\"x\",\"precedence\":1.5}}}")]
    [InlineData("{\"open\":\"(\",\"close\":\")\",\"operators\":{\"and\":{\"text\":\"x\",\"precedence\":1},\"and\":{\"text\":\"x\",\"precedence\":1}}}")]
    public void should_reject_invalid_closed_operator_rows(string table) => ((TemplateRefusal)Catch.Exception(() => OperatorTable.Parse(Encoding.UTF8.GetBytes(table)))).Diagnostic.Code.ShouldEqual(TemplateCodes.OperatorTable);

    [Fact]
    public void should_refuse_missing_operator_rows() => ((TemplateRefusal)Catch.Exception(() => OperatorTable.Parse(Encoding.UTF8.GetBytes("""{"open":"(","close":")","operators":{}}""")))).Diagnostic.Code.ShouldEqual(TemplateCodes.OperatorTable);

    [Theory]
    [InlineData("unknown", "Missing", "STAGE-DEGRADE-002")]
    [InlineData("throwing_operation_body", "Missing", "STAGE-DEGRADE-001")]
    [InlineData("throwing_operation_body", "Missing", "STAGE-DEGRADE-999")]
    public void should_refuse_gap_misuse(string role, string intent, string code)
    {
        using var plan = TemplateProfile.Prepare(Kit("{{ gap '" + role + "' '" + intent + "' 'model' '" + code + "' }}"), new given.a_language()).Templates!.BeginPlan(TemplateBudget.Default);
        plan.Render("main.scriban", JsonSerializer.SerializeToElement(new { model = new { } })).Refusal!.Code.ShouldEqual(TemplateCodes.Input);
    }

    [Theory]
    [InlineData(513, false)]
    [InlineData(20, true)]
    public void should_refuse_unbounded_or_multiline_gap_intent(int length, bool newline)
    {
        using var plan = TemplateProfile.Prepare(Kit("{{ gap 'disabled_specification' model.intent 'model' }}"), new given.a_language()).Templates!.BeginPlan(TemplateBudget.Default);
        var input = JsonSerializer.SerializeToElement(new { model = new { intent = new string('x', length) + (newline ? "\n" : string.Empty) } });
        plan.Render("main.scriban", input).Refusal!.Code.ShouldEqual(TemplateCodes.Input);
    }

    [Fact]
    public void should_keep_same_named_types_in_distinct_namespaces()
    {
        using var plan = TemplateProfile.Prepare(Kit("{{ for reference in model.types; type.ref reference; end }}"), new given.a_language()).Templates!.BeginPlan(TemplateBudget.Default);
        using var input = JsonDocument.Parse("""{"model":{"types":[{"kind":"concept","target":"customer","isCollection":false,"isOptional":false},{"kind":"composite","target":"other","isCollection":false,"isOptional":false}]}}""");
        var result = plan.Render("main.scriban", input.RootElement);
        result.Output!.Segments[0].ShouldEqual(new TemplateSymbolSegment(TemplateSymbolKind.Reference, new("Domain", "Customer")));
        result.Output.Segments[1].ShouldEqual(new TemplateSymbolSegment(TemplateSymbolKind.Reference, new("Other", "Customer")));
    }

    [Fact]
    public void should_lint_override_bodies_before_any_rendering()
    {
        var bodies = new TemplateSectionOverrides(new Dictionary<(string, string), string> { [("main.scriban", "body")] = "{{ os }}{{ date.now }}" });
        var prepared = TemplateProfile.Prepare(Kit("{{ wrap section 'body' }}x{{ end }}"), new given.a_language(), bodies);
        prepared.Templates.ShouldBeNull();
        prepared.Diagnostics.Length.ShouldEqual(2);
    }

    [Fact]
    public void should_reject_nested_sections_in_overrides()
    {
        var bodies = new TemplateSectionOverrides(new Dictionary<(string, string), string> { [("main.scriban", "body")] = "{{ wrap section 'inner' }}x{{ end }}" });
        TemplateProfile.Prepare(Kit("{{ wrap section 'body' }}x{{ end }}"), new given.a_language(), bodies).Diagnostics[0].Code.ShouldEqual(TemplateCodes.Helper);
    }

    [Fact]
    public void should_keep_section_names_isolated_between_templates()
    {
        var kit = when_rendering_partials.WithPartials(Kit("{{ wrap section 'body' }}a{{ end }}"), ("leaf", "{{ wrap section 'body' }}b{{ end }}"));
        var prepared = TemplateProfile.Prepare(kit, new given.a_language());
        prepared.Diagnostics.ShouldBeEmpty();
        prepared.Templates!.Sections.Count.ShouldEqual(2);
    }

    [Fact]
    public void should_refuse_unknown_primitive_spelling_without_leaking_a_clr_exception()
    {
        using var plan = TemplateProfile.Prepare(Kit("{{ type.ref model.type }}"), new given.a_language()).Templates!.BeginPlan(TemplateBudget.Default);
        using var input = JsonDocument.Parse("""{"model":{"type":{"kind":"primitive","primitive":"invented","isCollection":false,"isOptional":false}}}""");
        plan.Render("main.scriban", input.RootElement).Refusal!.Code.ShouldEqual(TemplateCodes.Input);
    }
}
