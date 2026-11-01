// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Xml.Linq;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_reaction_specifications;

public class with_native_bodies : a_generated_application
{
    string _passing = null!;
    string _mutation = null!;

    protected override ArtifactRenderPlan CreatePlan()
    {
        var plan = native_reaction_model.ExecutablePlan();
        Assert.Empty(plan.Refusals);
        return plan;
    }

    async Task Because()
    {
        await Run("native-build.log", "build", "InvoiceApp.csproj", "-c", "Debug", "-warnaserror", "--nologo");
        _passing = await Run("native-specs.log", "test", "InvoiceApp.csproj", "-c", "Debug", "--no-build", "--filter", "FullyQualifiedName!~mutated", "--logger", "trx;LogFileName=native.trx", "--results-directory", _evidence.FullName);
        _mutation = await Run("native-mutant.log", true, "test", "InvoiceApp.csproj", "-c", "Debug", "--no-build", "--filter", "FullyQualifiedName~mutated", "--logger", "trx;LogFileName=mutant.trx", "--results-directory", _evidence.FullName);
    }

    [Fact] void should_execute_inline_and_file_body_specifications() => _passing.ShouldContain("Failed:     0");

    [Fact]
    void should_execute_more_than_zero_generated_tests()
    {
        var results = Results("native.trx");
        Assert.Equal(20, results.Length);
        Assert.All(results, result => Assert.Equal("Passed", result.Outcome));
        Assert.Contains(results, result => result.Matches(className: "Invoices.CallForPapers.Review.Highlighting.when_high_score_highlights.when_high_score_highlights"));
        Assert.Contains(results, result => result.Matches(className: "Invoices.CallForPapers.Review.Highlighting.when_low_score_does_not_highlight.when_low_score_does_not_highlight"));
        Assert.Contains(results, result => result.Matches(className: "Invoices.CallForPapers.Review.FileHighlighting.when_high_score_file_highlights.when_high_score_file_highlights"));
    }

    [Fact]
    void should_fail_the_unchanged_payload_expectation_for_a_mutated_body()
    {
        _mutation.ShouldContain("Failed!");
        var results = Results("mutant.trx");
        Assert.Equal(10, results.Length);
        Assert.Equal(9, results.Count(result => result.Outcome == "Passed"));
        var failed = Assert.Single(results, result => result.Outcome == "Failed");
        Assert.Equal("Invoices.CallForPapers.Review.MutatedHighlighting.when_high_score_mutated_highlights.when_high_score_mutated_highlights", failed.ClassName);
        Assert.Equal("should_produce_event_1_with_score", failed.MethodName);
    }

    GeneratedTestResult[] Results(string file) => [.. GeneratedTestResults.Read(XDocument.Load(Path.Combine(_evidence.FullName, file)))];
}
#endif
