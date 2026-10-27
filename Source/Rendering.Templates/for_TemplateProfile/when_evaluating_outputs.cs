// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplateProfile;

public class when_evaluating_outputs : for_TemplatePreparer.given.a_kit
{
    static JsonElement Scope => JsonSerializer.SerializeToElement(new { model = new { name = "Customer", enabled = false, items = new[] { new { id = 1 }, new { id = 2 } } } });

    [Fact]
    public void should_use_locked_helpers_for_output_paths_and_conditions()
    {
        var prepared = TemplateProfile.Prepare(Kit(path: "{{ name.snake model.name }}.txt", when: "model.enabled", forEach: "model.items"), new given.a_language());
        using var plan = prepared.Prepared!.BeginPlan(TemplateBudget.Default);
        plan.RenderPath("main", Scope).Value.ShouldEqual("customer.txt");
        plan.EvaluateWhen("main", Scope).Value.ShouldBeFalse();
        var items = plan.EvaluateForEach("main", Scope);
        items.Diagnostics.ShouldBeEmpty();
        items.Value.Length.ShouldEqual(2);
        items.Value[1].GetProperty("id").GetInt64().ShouldEqual(2);
    }

    [Fact]
    public void should_use_defaults_for_absent_expressions()
    {
        using var plan = TemplateProfile.Prepare(Kit(), new given.a_language()).Prepared!.BeginPlan(TemplateBudget.Default);
        plan.EvaluateWhen("main", Scope).Value.ShouldBeTrue();
        plan.EvaluateForEach("main", Scope).Value[0].GetRawText().ShouldEqual(Scope.GetRawText());
    }

    [Theory]
    [InlineData("when")]
    [InlineData("forEach")]
    [InlineData("path")]
    public void should_latch_unknown_output_ids(string operation)
    {
        using var plan = TemplateProfile.Prepare(Kit(), new given.a_language()).Prepared!.BeginPlan(TemplateBudget.Default);
        var code = operation switch
        {
            "when" => plan.EvaluateWhen("missing", Scope).Diagnostics[0].Code,
            "forEach" => plan.EvaluateForEach("missing", Scope).Diagnostics[0].Code,
            _ => plan.RenderPath("missing", Scope).Diagnostics[0].Code
        };
        code.ShouldEqual(TemplateCodes.Input);
        plan.IsRefused.ShouldBeTrue();
        plan.EvaluateWhen("main", Scope).Diagnostics[0].Code.ShouldEqual(TemplateCodes.AlreadyRefused);
    }

    [Fact]
    public void should_bound_aggregate_fan_out()
    {
        using var plan = TemplateProfile.Prepare(Kit(forEach: "model.items"), new given.a_language()).Prepared!.BeginPlan(TemplateBudget.Default with { MaxFanOut = 3 });
        plan.EvaluateForEach("main", Scope).Succeeded.ShouldBeTrue();
        plan.EvaluateForEach("main", Scope).Diagnostics[0].Code.ShouldEqual(TemplateCodes.FanOut);
    }

    [Fact]
    public void should_aggregate_preparation_errors_in_inactive_sources()
    {
        var kit = when_rendering_partials.WithPartials(Kit("{{ date.now }}", when: "false", forEach: "regex.match model.name 'a'"), ("unused", "{{ model.x = 1 }}"));
        var result = TemplateProfile.Prepare(kit, new given.a_language());
        result.Prepared.ShouldBeNull();
        result.Diagnostics.Length.ShouldEqual(3);
    }
}
