// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticGeneratedCommandSpecification;

public class when_rendering_unpinned_time_ordered_values : Specification
{
    [Theory]
    [InlineData("sortableId", "01kdyak3se")]
    [InlineData("userId", "user_01kdyak3se")]
    [InlineData("bareTypeId", "01kdyak3se")]
    public void should_fix_the_clock_before_setup_and_assert_the_receipt_time(string name, string prefix)
    {
        var source = Render(name);
        source.ShouldContain("void Establish()\n    {\n        _scenario.Clock(new DateTimeOffset(2026, 1, 2, 3, 4, 5, 678, TimeSpan.Zero));");
        source.ShouldContain($"should_generate_{Naming.Identifiers.ToSnakeCase(name)}_at_the_receipt_time()");
        source.ShouldContain($"StartsWith(\"{prefix}\", StringComparison.Ordinal).ShouldBeTrue()");
        source.ShouldContain("_scenario.AllowGeneration();");
    }

    // Pinned fixtures retain their generated values without enabling receipt-time generation.
    [Fact] void should_preserve_the_pinned_specification_bytes() => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(LegacyLicenseHeader.Restore("when_several_initial_facts.cs", Render(null))))).ShouldEqual("eddc079d46770dd58637773689498409ca964c51ad4b12a9af69b039dd1d087e");
    [Fact] void should_leave_pinned_specifications_without_clock_setup() => Render(null).ShouldNotContain("_scenario.Clock(");
    [Fact] void should_leave_pinned_specifications_without_receipt_time_facts() => Render(null).ShouldNotContain("_at_the_receipt_time()");
    [Fact] void should_leave_non_time_ordered_generation_without_clock_setup() => Render("receipt").ShouldNotContain("_scenario.Clock(");

    [Fact]
    void should_use_the_authored_clock_for_time_ordered_generation()
    {
        const string clock = "2027-03-04T10:00:00.1230000Z";
        var source = Render("sortableId", clock);
        source.ShouldContain("_scenario.Clock(new DateTimeOffset(639397512001230000L, TimeSpan.Zero));");
        source.ShouldContain($"StartsWith(\"{SemanticGenerationTime.Prefix(a_generation_model.Model().Application.Concepts.Single(concept => concept.Name == "SortableId"), DateTimeOffset.Parse(clock, System.Globalization.CultureInfo.InvariantCulture))}\"");
        source.ShouldNotContain("01kdyak3se");
    }

    static string Render(string? unpinned, string? clock = null)
    {
        var original = a_generation_model.Model();
        var module = original.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single(value => value.Name == "Start");
        var specification = slice.Specifications.Single(value => value.Name == "SeveralInitialFacts");
        var property = slice.Commands.Single().Properties.SingleOrDefault(value => value.Name == unpinned);
        var changed = specification with
        {
            GivenClock = clock,
            When = specification.When with { GeneratedValues = [.. specification.When.GeneratedValues.Where(value => value.TargetProperty != property?.Id)] }
        };
        var model = ExecutableSemanticModel.Create(original.LanguageVersion, original.SemanticVersion, original.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(value => value.Id == slice.Id ? value with { Specifications = [changed] } : value)] }] }]
        });
        var plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("GenerationApp", "GenerationApp"));
        Assert.True(plan.Success, string.Join(';', plan.Diagnostics.Select(value => value.Message)));

        return Encoding.UTF8.GetString(plan.Artifacts.Single(value => value.RelativePath.EndsWith("when_several_initial_facts.cs", StringComparison.Ordinal)).Bytes.AsSpan());
    }
}
