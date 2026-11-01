// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Xml.Linq;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_reducer_fact_specifications : a_generated_application
{
    protected override bool DeferPlan => true;

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task should_execute_the_rendered_body_and_detect_a_wrong_transition(bool self, bool wrongBody)
    {
        try
        {
            var source = wrongBody ? a_reducer_fact_specification.Source.Replace(
                a_reducer_fact_specification.Body,
                "return new TalkScore(Guid.Parse(context.Key), (context.State?.Score ?? 0) + context.Event.Score + 1);",
                StringComparison.Ordinal)
                : a_reducer_fact_specification.Source;
            var plan = await a_reducer_fact_specification.Plan(source, self);
            RenderApplication(plan);
            var specs = GeneratedFiles("when_score_*.cs");
            Assert.Equal(3, specs.Length);
            Assert.All(specs, path => Assert.Contains("ReadModelScenario<", ReadGeneratedFile(path), StringComparison.Ordinal));
            Assert.Contains("TalkScored(5", ReadGeneratedFile("CallForPapers/Review/TalkScores/when_score_after_append_is_projected.cs"), StringComparison.Ordinal);
            var build = await Run("reducer-facts-build.log", "build", "Projects.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
            var trx = Path.Combine(_evidence.FullName, "reducer-facts.trx");
            await Run("reducer-facts-test.log", wrongBody, "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo", "--logger", $"trx;LogFileName={trx}", "--filter", "FullyQualifiedName~when_score_");
            var results = GeneratedTestResults.Read(XDocument.Load(trx));
            Assert.NotEmpty(results);
            foreach (var name in new[] { "when_score_shows", "when_score_accumulates", "when_score_after_append" })
            {
                var assertions = ResultsForSpecification(results, name);
                Assert.NotEmpty(assertions);
                if (wrongBody) Assert.Contains(assertions, result => result.Outcome == "Failed");
                else Assert.All(assertions, result => Assert.Equal("Passed", result.Outcome));
            }
        }
        finally
        {
            Cleanup();
        }
    }

    internal static GeneratedTestResult[] ResultsForSpecification(IEnumerable<GeneratedTestResult> results, string name) =>
        [.. results.Where(result => result.Matches(className: name, match: GeneratedTestNameMatch.Prefix))];
}
#endif
