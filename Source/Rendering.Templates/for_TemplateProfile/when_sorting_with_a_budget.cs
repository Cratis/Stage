// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplateProfile;

public class when_sorting_with_a_budget : for_TemplatePreparer.given.a_kit
{
    [Theory]
    [InlineData(100, 100)]
    [InlineData(50, 200)] // Materialization and result construction alone fit; comparisons must exhaust the budget.
    public void should_meter_materialization_and_comparisons_and_drop_partial_output(int size, int maxSteps)
    {
        using var plan = TemplateProfile.Prepare(Kit("before{{ sorted = array.sort (1.." + size + ") }}after"), new given.a_language()).Templates.BeginPlan(TemplateBudget.Default with { MaxSteps = maxSteps });
        var clock = Stopwatch.StartNew();
        var result = plan.Render("main.scriban", JsonSerializer.SerializeToElement(new { model = new { } }));
        result.Refusal.ShouldNotBeNull();
        result.Refusal.Code.ShouldEqual(TemplateCodes.Steps);
        result.Output.ShouldBeNull();
        (clock.Elapsed < TimeSpan.FromSeconds(5)).ShouldBeTrue();
    }

    [Fact]
    public void should_finish_the_proposed_extreme_range_without_overflow()
    {
        using var plan = TemplateProfile.Prepare(Kit("{{ sorted = array.sort (9223372036854775806..9223372036854775807); sorted.size }}"), new given.a_language()).Templates.BeginPlan(TemplateBudget.Default with { MaxSteps = 100 });
        var clock = Stopwatch.StartNew();
        var result = plan.Render("main.scriban", JsonSerializer.SerializeToElement(new { model = new { } }));
        result.Refusal.ShouldBeNull();
        result.Output.ShouldNotBeNull();
        string.Concat(result.Output.Segments.OfType<TemplateTextSegment>().Select(segment => segment.Text)).ShouldEqual("2");
        (clock.Elapsed < TimeSpan.FromSeconds(5)).ShouldBeTrue();
    }

    [Fact]
    public void should_finish_the_exact_proposed_overflow_reproducer()
    {
        using var plan = TemplateProfile.Prepare(Kit("{{ array.sort (9223372036854775806..9223372036854775807) }}"), new given.a_language()).Templates.BeginPlan(TemplateBudget.Default with { MaxSteps = 100 });
        var clock = Stopwatch.StartNew();
        var result = plan.Render("main.scriban", JsonSerializer.SerializeToElement(new { model = new { } }));
        result.Refusal.ShouldBeNull();
        result.Output.ShouldNotBeNull();
        (clock.Elapsed < TimeSpan.FromSeconds(5)).ShouldBeTrue();
    }

    [Fact]
    public void should_honor_the_existing_range_size_guard()
    {
        using var plan = TemplateProfile.Prepare(Kit("before{{ sorted = array.sort (1..10000) }}after"), new given.a_language()).Templates.BeginPlan(TemplateBudget.Default with { MaxSteps = 100 });
        var result = plan.Render("main.scriban", JsonSerializer.SerializeToElement(new { model = new { } }));
        result.Refusal.ShouldNotBeNull();
        result.Refusal.Code.ShouldEqual(TemplateCodes.Steps);
        result.Output.ShouldBeNull();
    }
}
