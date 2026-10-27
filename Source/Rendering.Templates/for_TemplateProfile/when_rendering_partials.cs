// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Kits;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplateProfile;

public class when_rendering_partials : for_TemplatePreparer.given.a_kit
{
    static JsonElement Scope => JsonSerializer.SerializeToElement(new { model = new { name = "Shared" } });

    internal static LoadedKit WithPartials(LoadedKit kit, params (string Name, string Text)[] partials) => kit with
    {
        Manifest = kit.Manifest with { Partials = [.. partials.Select(partial => new KitPartial(partial.Name, partial.Name + ".scriban"))] },
        Files = new KitFiles(kit.Files.Paths.Select(path => new KeyValuePair<string, ImmutableArray<byte>>(path, kit.Files[path]))
            .Concat(partials.Select(partial => new KeyValuePair<string, ImmutableArray<byte>>(partial.Name + ".scriban", [.. Encoding.UTF8.GetBytes(partial.Text)]))))
    };

    [Fact]
    public void should_render_nested_declared_partials_in_the_same_context()
    {
        var kit = WithPartials(Kit("A{{ render 'outer' model }}Z"), ("outer", "{{ item.name }}{{ render 'inner' item }}"), ("inner", ":{{ item.name }}"));
        using var plan = TemplateProfile.Prepare(kit, new given.a_language()).Prepared!.BeginPlan(TemplateBudget.Default);
        var result = plan.Render("main.scriban", Scope);
        result.Refusal.ShouldBeNull();
        ((TemplateTextSegment)result.Output!.Segments[0]).Text.ShouldEqual("AShared:SharedZ");
    }

    [Fact]
    public void should_apply_a_validated_section_override()
    {
        var kit = WithPartials(Kit("{{ section 'body' 'default' }}"), ("default", "original"), ("replacement", "changed"));
        var overrides = ContractOverrides.None with { Sections = ImmutableDictionary<string, string>.Empty.Add("main.scriban#body", "replacement") };
        using var plan = TemplateProfile.Prepare(kit, new given.a_language(), overrides).Prepared!.BeginPlan(TemplateBudget.Default);
        var result = plan.Render("main.scriban", Scope);
        result.Refusal.ShouldBeNull();
        ((TemplateTextSegment)result.Output!.Segments[0]).Text.ShouldEqual("changed");
    }

    [Fact]
    public void should_refuse_unknown_overrides_before_rendering()
    {
        var overrides = ContractOverrides.None with { Sections = ImmutableDictionary<string, string>.Empty.Add("main.scriban#missing", "replacement") };
        TemplateProfile.Prepare(Kit(), new given.a_language(), overrides).Diagnostics[0].Code.ShouldEqual(TemplateCodes.Override);
    }

    [Fact]
    public void should_bound_cyclic_partial_recursion()
    {
        var kit = WithPartials(Kit("{{ render 'cycle' model }}"), ("cycle", "{{ render 'cycle' item }}"));
        using var plan = TemplateProfile.Prepare(kit, new given.a_language()).Prepared!.BeginPlan(TemplateBudget.Default with { MaxRecursion = 4 });
        var result = plan.Render("main.scriban", Scope);
        result.Output.ShouldBeNull();
        result.Refusal!.Code.ShouldEqual(TemplateCodes.Recursion);
    }

    [Theory]
    [InlineData("DenyEnclosingOperation")]
    [InlineData("DisabledSpecification")]
    [InlineData("UnregisteredConsumer")]
    [InlineData("RetainedSignatureOrType")]
    [InlineData("GraphProvenOmittedLeaf")]
    public void should_record_typed_gaps_without_latching_refusal(string role)
    {
        using var plan = TemplateProfile.Prepare(Kit("{{ gap 'APP-1' '" + role + "' 'Missing behavior' 'model.x' }}"), new given.a_language()).Prepared!.BeginPlan(TemplateBudget.Default);
        var result = plan.Render("main.scriban", Scope);
        result.Refusal.ShouldBeNull();
        result.Output!.Gaps[0].ModelPath.ShouldEqual("model.x");
        plan.IsRefused.ShouldBeFalse();
    }

    [Fact]
    public void should_latch_explicit_refusal()
    {
        using var plan = TemplateProfile.Prepare(Kit("before{{ refuse 'KIT-1' 'unsafe' }}after"), new given.a_language()).Prepared!.BeginPlan(TemplateBudget.Default);
        var result = plan.Render("main.scriban", Scope);
        result.Output.ShouldBeNull();
        result.Refusal!.Code.ShouldEqual(TemplateCodes.Refuse);
        plan.IsRefused.ShouldBeTrue();
    }
}
