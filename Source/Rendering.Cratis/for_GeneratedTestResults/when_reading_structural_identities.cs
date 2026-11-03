// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Xml.Linq;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedTestResults;

public class when_reading_structural_identities : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_join_results_by_id_and_preserve_order_counts_and_outcomes(bool readableNames)
    {
        var results = GeneratedTestResults.Read(Report(readableNames));
        results.ShouldEqualInOrder([
            new("mutant", "Example.when_mutated", "should_produce_event_1_with_score", "Failed"),
            new("native", "Example.when_native", "should_produce_event_1_with_score", "Passed"),
            new("native", "Example.when_native", "should_produce_event_1_with_score", "Passed")
        ]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_match_only_the_requested_structural_identity_and_outcome(bool readableNames)
    {
        var results = GeneratedTestResults.Read(Report(readableNames));
        results.Where(result => result.Matches(className: "when_native", methodName: "should_produce_event_1_with_score")).Select(result => result.Outcome).ShouldContainOnly("Passed", "Passed");
        results.Single(result => result.Matches(className: "Example.when_mutated", methodName: "should_produce_event_1_with_score")).Outcome.ShouldEqual("Failed");
        results.Single(result => result.Matches(className: "when_mutated", methodName: "should_produce_event_", match: GeneratedTestNameMatch.Prefix)).Outcome.ShouldEqual("Failed");
        results.Single(result => result.Matches(fullName: "Example.when_mutated.should_produce_event_1_with_score")).Outcome.ShouldEqual("Failed");
        results.Any(result => result.Matches(className: "when_nativ")).ShouldBeFalse();
        results.Any(result => result.Matches(className: "Other.Example.when_native")).ShouldBeFalse();
        results.Any(result => result.Matches(className: "when_native", methodName: "should_produce_event_1_with_scor")).ShouldBeFalse();
        results.Any(result => result.Matches(className: "Other.", match: GeneratedTestNameMatch.Prefix)).ShouldBeFalse();
        results.Any(result => result.Matches(fullName: "Other.when_mutated.should_produce_event_1_with_score")).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_not_fall_back_to_display_names_for_an_unmatched_result(bool readableNames)
    {
        var document = Report(readableNames);
        XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
        document.Descendants(ns + "UnitTestResult").First().SetAttributeValue("testId", "missing");
        Assert.Throws<KeyNotFoundException>(() => GeneratedTestResults.Read(document));
    }

    static XDocument Report(bool readableNames)
    {
        var displayName = readableNames
            ? "when native › should produce event 1 with score"
            : "Example.when_native.should_produce_event_1_with_score";
        return XDocument.Parse($"""
            <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
              <Results>
                <UnitTestResult testId="mutant" testName="{displayName}" outcome="Failed" />
                <UnitTestResult testId="native" testName="{displayName}" outcome="Passed" />
                <UnitTestResult testId="native" testName="{displayName}" outcome="Passed" />
              </Results>
              <TestDefinitions>
                <UnitTest id="native" name="{displayName}">
                  <TestMethod className="Example.when_native" name="should_produce_event_1_with_score" />
                </UnitTest>
                <UnitTest id="mutant" name="{displayName}">
                  <TestMethod className="Example.when_mutated" name="should_produce_event_1_with_score" />
                </UnitTest>
              </TestDefinitions>
            </TestRun>
            """);
    }
}
#endif
