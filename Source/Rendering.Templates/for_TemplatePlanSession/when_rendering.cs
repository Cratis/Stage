// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplatePlanSession;

public class when_rendering : for_TemplatePreparer.given.a_kit
{
    protected static JsonElement Scope => JsonDocument.Parse("{\"model\":{\"name\":\"World\"}}").RootElement.Clone();

    [Fact]
    public void should_render_identically_across_artifacts()
    {
        using var session = TemplatePreparer.Prepare(Kit()).Value!.BeginPlan(TemplateBudget.Default);
        var first = session.Render("main.scriban", Scope);
        first.Refusal.ShouldBeNull();
        ((TemplateTextSegment)first.Output!.Segments[0]).Text.ShouldEqual(((TemplateTextSegment)session.Render("main.scriban", Scope).Output!.Segments[0]).Text);
    }

    [Fact]
    public void should_refuse_clr_member_access_and_latch()
    {
        using var session = TemplatePreparer.Prepare(Kit("{{ model.name.length }}")).Value!.BeginPlan(TemplateBudget.Default);
        var result = session.Render("main.scriban", Scope);
        result.Refusal!.Code.ShouldEqual(TemplateCodes.HostValue);
        result.Output.ShouldBeNull();
        session.Render("main.scriban", Scope).Refusal!.Code.ShouldEqual(TemplateCodes.AlreadyRefused);
    }

    [Theory]
    [InlineData("{{ while true }}x{{ end }}", TemplateCodes.Steps)]
    [InlineData("{{ for i in 1..10000000 }}x{{ end }}", TemplateCodes.Steps)]
    [InlineData("{{ x = 'ab'; for i in 1..20; x = x + x; end; x }}", TemplateCodes.StringLength)]
    public void should_bound_adversarial_templates(string text, string code)
    {
        using var session = TemplatePreparer.Prepare(Kit(text)).Value!.BeginPlan(TemplateBudget.Default with { MaxSteps = 2000, MaxStringLength = 128 });
        var result = session.Render("main.scriban", Scope);
        result.Output.ShouldBeNull();
        result.Refusal!.Code.ShouldEqual(code);
    }

    [Fact]
    public void should_count_aggregate_utf8_output()
    {
        using var session = TemplatePreparer.Prepare(Kit("éé")).Value!.BeginPlan(TemplateBudget.Default with { MaxOutputBytes = 6 });
        session.Render("main.scriban", Scope).Output.ShouldNotBeNull();
        var refused = session.Render("main.scriban", Scope);
        refused.Refusal!.Code.ShouldEqual(TemplateCodes.Output);
        refused.Output.ShouldBeNull();
    }

    [Fact]
    public void should_count_artifacts()
    {
        using var session = TemplatePreparer.Prepare(Kit()).Value!.BeginPlan(TemplateBudget.Default with { MaxArtifacts = 1 });
        session.Render("main.scriban", Scope);
        session.Render("main.scriban", Scope).Refusal!.Code.ShouldEqual(TemplateCodes.Artifacts);
    }
}
