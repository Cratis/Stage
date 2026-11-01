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
        Assert.NotEmpty(results);
        Assert.All(results, result => Assert.Equal("Passed", result.Attribute("outcome")!.Value));
        Assert.Contains(results, result => result.Attribute("testName")!.Value.Contains("high_score", StringComparison.Ordinal));
        Assert.Contains(results, result => result.Attribute("testName")!.Value.Contains("low_score", StringComparison.Ordinal));
        Assert.Contains(results, result => result.Attribute("testName")!.Value.Contains("file_highlight", StringComparison.Ordinal));
    }

    [Fact]
    void should_fail_the_unchanged_payload_expectation_for_a_mutated_body()
    {
        _mutation.ShouldContain("Failed!");
        Assert.Contains(Results("mutant.trx"), result => result.Attribute("outcome")!.Value == "Failed" &&
            result.Attribute("testName")!.Value.EndsWith("should_produce_event_1_with_score", StringComparison.Ordinal));
    }

    XElement[] Results(string file)
    {
        XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
        return [.. XDocument.Load(Path.Combine(_evidence.FullName, file)).Descendants(ns + "UnitTestResult")];
    }
}
#endif
