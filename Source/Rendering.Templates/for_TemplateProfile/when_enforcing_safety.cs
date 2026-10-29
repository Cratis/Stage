// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplateProfile;

public class when_enforcing_safety : for_TemplatePreparer.given.a_kit
{
    static JsonElement Scope => JsonSerializer.SerializeToElement(new { model = new { name = "data" } });

    [Theory]
    [InlineData("{{ object.from_json '{}' }}")]
    [InlineData("{{ object.eval '1' }}")]
    [InlineData("{{ include 'file' }}")]
    [InlineData("{{ os }}")]
    [InlineData("{{ math.random 0 1 }}")]
    [InlineData("{{ string.split '' '' | object.eval }}")]
    [InlineData("{{ x = object; x.eval '1' }}")]
    [InlineData("{{ string.eval '1' }}")]
    [InlineData("{{ regex.match 'x' '.*' }}")]
    public void should_deny_ambient_or_dynamic_surfaces(string text) => TemplateProfile.Prepare(Kit(text), new given.a_language()).Diagnostics[0].Code.ShouldEqual(TemplateCodes.Allowlist);

    [Fact]
    public void should_bound_nested_blocks_before_parsing()
    {
        var text = string.Concat(Enumerable.Repeat("{{ if true }}", 33)) + string.Concat(Enumerable.Repeat("{{ end }}", 33));
        TemplateProfile.Prepare(Kit(text), new given.a_language()).Diagnostics[0].Code.ShouldEqual(TemplateCodes.Nesting);
    }

    [Fact]
    public void should_bound_deadline_and_drop_partial_output()
    {
        using var plan = TemplateProfile.Prepare(Kit("before{{ while true }}x{{ end }}"), new given.a_language()).Templates!.BeginPlan(TemplateBudget.Default with { Deadline = TimeSpan.FromTicks(1) });
        var result = plan.Render("main.scriban", Scope);
        result.Refusal!.Code.ShouldEqual(TemplateCodes.Deadline);
        result.Output.ShouldBeNull();
    }

    [Theory]
    [InlineData("{{ sym.ref 'Domain' 'Customer' }}")]
    [InlineData("{{ sym.imports }}")]
    [InlineData("{{ gap 'throwing_operation_body' 'Missing' 'model' }}")]
    public void should_reject_structured_paths(string path)
    {
        using var plan = TemplateProfile.Prepare(Kit(path: path), new given.a_language()).Templates!.BeginPlan(TemplateBudget.Default);
        var result = plan.RenderPath("main", Scope);
        result.Refusal!.Code.ShouldEqual(TemplateCodes.Input);
        result.Value.ShouldBeNull();
    }

    [Fact]
    public void should_keep_fan_out_types_strict()
    {
        using var plan = TemplateProfile.Prepare(Kit(forEach: "model.name"), new given.a_language()).Templates!.BeginPlan(TemplateBudget.Default);
        plan.EvaluateForEach("main", Scope).Refusal!.Code.ShouldEqual(TemplateCodes.Input);
    }

    [Fact]
    public void should_keep_conditions_strict()
    {
        using var plan = TemplateProfile.Prepare(Kit(when: "model.name"), new given.a_language()).Templates!.BeginPlan(TemplateBudget.Default);
        plan.EvaluateWhen("main", Scope).Refusal!.Code.ShouldEqual(TemplateCodes.Input);
    }

    [Fact]
    public void should_support_local_functions_without_host_bindings()
    {
        using var plan = TemplateProfile.Prepare(Kit("{{ func greet(x); 'Hello ' + x; end; greet model.name }}"), new given.a_language()).Templates!.BeginPlan(TemplateBudget.Default);
        var result = plan.Render("main.scriban", Scope);
        result.Refusal.ShouldBeNull();
        ((TemplateTextSegment)result.Output!.Segments[0]).Text.ShouldEqual("Hello data");
    }
}
