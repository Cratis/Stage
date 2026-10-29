// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Kits;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplateProfile;

public class when_lowering : for_TemplatePreparer.given.a_kit
{
    internal const string Operators = """{"open":"(","close":")","operators":{"and":{"text":" && ","precedence":20},"or":{"text":" || ","precedence":10},"equal":{"text":" == ","precedence":30},"notEqual":{"text":" != ","precedence":30},"greaterThan":{"text":" > ","precedence":30},"greaterThanOrEqual":{"text":" >= ","precedence":30},"lessThan":{"text":" < ","precedence":30},"lessThanOrEqual":{"text":" <= ","precedence":30}}}""";

    internal static LoadedKit WithOperators(LoadedKit kit) => when_rendering_partials.WithPartials(kit, ("leaf", "{{ sym.ref 'Domain' item.identifier }}")) with
    {
        Manifest = kit.Manifest with { Partials = [new("leaf", "leaf.scriban")], Tables = [new("operators", "operators.json")] },
        Files = new KitFiles(kit.Files.Paths.Select(path => new KeyValuePair<string, ImmutableArray<byte>>(path, kit.Files[path]))
            .Append(new("leaf.scriban", [.. Encoding.UTF8.GetBytes("{{ sym.ref 'Domain' item.identifier }}")]))
            .Append(new("operators.json", [.. Encoding.UTF8.GetBytes(Operators)])))
    };

    [Theory]
    [InlineData("equal", "or", "(1 || 2) == 3")]
    [InlineData("or", "and", "1 && 2 || 3")]
    public void should_preserve_left_precedence(string parent, string child, string expected)
    {
        var preparation = TemplateProfile.Prepare(WithOperators(Kit("{{ expr.lower model.expression 'operators' 'leaf' 'throwing_operation_body' }}")), new given.a_language());
        preparation.Diagnostics.ShouldBeEmpty();
        using var plan = preparation.Templates!.BeginPlan(TemplateBudget.Default);
        using var json = JsonDocument.Parse("{\"model\":{\"expression\":{\"$kind\":\"comparison\",\"operator\":\"" + parent + "\",\"left\":{\"$kind\":\"logical\",\"operator\":\"" + child + "\",\"left\":{\"$kind\":\"value\",\"literal\":{\"$kind\":\"number\",\"value\":1}},\"right\":{\"$kind\":\"value\",\"literal\":{\"$kind\":\"number\",\"value\":2}}},\"right\":{\"$kind\":\"value\",\"literal\":{\"$kind\":\"number\",\"value\":3}}}}}");
        var result = plan.Render("main.scriban", json.RootElement);
        result.Refusal.ShouldBeNull();
        ((TemplateTextSegment)result.Output!.Segments[0]).Text.ShouldEqual(expected);
    }

    [Theory]
    [InlineData("or", "or", "1 || (2 || 3)")]
    [InlineData("and", "or", "1 && (2 || 3)")]
    [InlineData("or", "and", "1 || 2 && 3")]
    public void should_preserve_the_right_operand_tree(string parent, string child, string expected)
    {
        using var plan = TemplateProfile.Prepare(WithOperators(Kit("{{ expr.lower model.expression 'operators' 'leaf' 'throwing_operation_body' }}")), new given.a_language()).Templates!.BeginPlan(TemplateBudget.Default);
        using var json = JsonDocument.Parse("""{"model":{"expression":{"$kind":"logical","operator":"PARENT","left":{"$kind":"value","literal":{"$kind":"number","value":1}},"right":{"$kind":"logical","operator":"CHILD","left":{"$kind":"value","literal":{"$kind":"number","value":2}},"right":{"$kind":"value","literal":{"$kind":"number","value":3}}}}}}""".Replace("PARENT", parent, StringComparison.Ordinal).Replace("CHILD", child, StringComparison.Ordinal));
        var result = plan.Render("main.scriban", json.RootElement);
        result.Refusal.ShouldBeNull();
        ((TemplateTextSegment)result.Output!.Segments[0]).Text.ShouldEqual(expected);
    }

    [Theory]
    [InlineData("{\"$kind\":\"text\",\"value\":\"hello\"}", "\"hello\"")]
    [InlineData("{\"$kind\":\"number\",\"value\":42}", "42")]
    [InlineData("{\"$kind\":\"number\",\"value\":1.25}", "1.25")]
    [InlineData("{\"$kind\":\"boolean\",\"value\":true}", "yes")]
    [InlineData("{\"$kind\":\"null\"}", "nil")]
    public void should_delegate_scalar_value_spelling(string literal, string expected)
    {
        using var plan = TemplateProfile.Prepare(WithOperators(Kit("{{ expr.lower model.expression 'operators' 'leaf' 'throwing_operation_body' }}")), new given.a_language()).Templates!.BeginPlan(TemplateBudget.Default);
        using var json = JsonDocument.Parse("{\"model\":{\"expression\":{\"$kind\":\"value\",\"literal\":" + literal + "}}}");
        ((TemplateTextSegment)plan.Render("main.scriban", json.RootElement).Output!.Segments[0]).Text.ShouldEqual(expected);
    }

    [Fact]
    public void should_preserve_structured_leaf_output()
    {
        using var plan = TemplateProfile.Prepare(WithOperators(Kit("{{ expr.lower model.expression 'operators' 'leaf' 'throwing_operation_body' }}")), new given.a_language()).Templates!.BeginPlan(TemplateBudget.Default);
        using var json = JsonDocument.Parse("""{"model":{"expression":{"$kind":"resolved","identifier":"Customer"}}}""");
        plan.Render("main.scriban", json.RootElement).Output!.Segments[0].ShouldEqual(new TemplateSymbolSegment(TemplateSymbolKind.Reference, new("Domain", "Customer")));
    }

    [Fact]
    public void should_degrade_unsupported_semantics_without_refusing()
    {
        using var plan = TemplateProfile.Prepare(WithOperators(Kit("{{ expr.lower model.expression 'operators' 'leaf' 'throwing_operation_body' }}")), new given.a_language()).Templates!.BeginPlan(TemplateBudget.Default);
        using var json = JsonDocument.Parse("""{"model":{"expression":{"$kind":"call"}}}""");
        var result = plan.Render("main.scriban", json.RootElement);
        result.Refusal.ShouldBeNull();
        result.Output!.Gaps[0].Role.ShouldEqual(GapRole.ThrowingOperationBody);
        ((TemplateTextSegment)result.Output.Segments[0]).Text.ShouldEqual("FAIL[ThrowingOperationBody:STAGE-DEGRADE-001]");
    }
}
