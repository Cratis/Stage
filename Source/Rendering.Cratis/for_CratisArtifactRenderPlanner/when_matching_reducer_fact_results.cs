// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Xml.Linq;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_matching_reducer_fact_results : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_match_each_case_by_structural_identity_and_preserve_its_outcomes(bool readableNames)
    {
        foreach (var name in new[] { "when_score_shows", "when_score_accumulates", "when_score_after_append" })
        {
            var results = GeneratedTestResults.Read(Report(name, readableNames));
            var assertions = when_executing_reducer_fact_specifications.ResultsForSpecification(results, name);
            assertions.ShouldEqualInOrder([
                new("talk-id", $"Projects.{name}_is_projected.{name}_is_projected", "should_project_talk_id", "Passed"),
                new("score", $"Projects.{name}_is_projected.{name}_is_projected", "should_project_score", "Failed")
            ]);
            when_executing_reducer_fact_specifications.ResultsForSpecification(results, "when_missing").ShouldBeEmpty();
        }
    }

    static XDocument Report(string name, bool readableNames)
    {
        var displayName = readableNames
            ? $"{name.Replace('_', ' ')} is projected › should project score"
            : $"Projects.{name}_is_projected.{name}_is_projected.should_project_score";
        return XDocument.Parse($"""
            <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
              <Results>
                <UnitTestResult testId="talk-id" testName="{displayName}" outcome="Passed" />
                <UnitTestResult testId="unrelated" testName="{displayName}" outcome="Passed" />
                <UnitTestResult testId="score" testName="{displayName}" outcome="Failed" />
              </Results>
              <TestDefinitions>
                <UnitTest id="score" name="{displayName}">
                  <TestMethod className="Projects.{name}_is_projected.{name}_is_projected" name="should_project_score" />
                </UnitTest>
                <UnitTest id="unrelated" name="{displayName}">
                  <TestMethod className="Projects.when_unrelated" name="should_project_score" />
                </UnitTest>
                <UnitTest id="talk-id" name="{displayName}">
                  <TestMethod className="Projects.{name}_is_projected.{name}_is_projected" name="should_project_talk_id" />
                </UnitTest>
              </TestDefinitions>
            </TestRun>
            """);
    }
}
#endif
