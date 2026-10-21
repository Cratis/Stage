// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Proves that a missing read-model instance rejects the triggering command even when the command its reaction invokes reads the same view by
/// the same key and nothing the cascade appends removes it: the specification renders and passes on the reference evaluator, the Stage executor
/// and the rendered application.
/// </summary>
/// <param name="fixture">The three-way fixture.</param>
/// <param name="output">The test output.</param>
public class when_comparing_rejections_before_a_reaction_with_a_shared_read(when_comparing_rejections_before_a_reaction_with_a_shared_read.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_rejections_before_a_reaction_with_a_shared_read.context>
{
    const int SpecificationCount = 1;

    [Fact] void should_compare_every_specification() => fixture.Outcomes.Length.ShouldEqual(SpecificationCount);
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_have_no_differences() => fixture.Differences.ShouldBeEmpty();
    [Fact] void should_pass_every_outcome_on_every_path() => fixture.Outcomes.Count(outcome => outcome.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(SpecificationCount);

    public class context : a_three_way_application
    {
        const string Source = """
            command reads protected
            module Plants
              feature Care
                slice StateChange Tagging
                  event PlantTagged
                  command TagPlant
                    id String identifier
                    reads PlantRecord by id
                    produces PlantTagged
                      for id
                slice StateView Records
                  event PlantPlaced
                    id String
                  event PlantRetired
                  readmodel PlantRecord
                    id String key
                    inspected Bool
                  projection BuildRecord => PlantRecord
                    from PlantPlaced
                      id = $eventSourceId
                    from PlantTagged
                      inspected = true
                    remove with PlantRetired
                  query RecordById => PlantRecord optional
                    by id String
                slice StateChange Inspecting
                  event PlantInspected
                    id String
                  command InspectPlant
                    id String identifier
                    reads PlantRecord by id
                    produces PlantInspected
                      for id
                      id = id
                  specification InspectingAnUnknownPlant
                    when InspectPlant
                      id = "unknown"
                    then error "PlantRecord 'unknown' does not exist."
                slice Automation TagAfterInspecting
                  reaction TagInspected
                    when PlantInspected
                      id
                      invokes TagPlant
                        id = id
            """;

        protected override string ApplicationName => "SharedReadRejectionBeforeReaction";
        protected override string ProjectFile => "SharedReadRejectionBeforeReaction.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("source", Compile())];
        Task Because() => Verify();

        static ExecutableSemanticModel Compile()
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("SharedReadRejectionBeforeReaction"));
            var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", Source);
            var compilation = new SemanticModelCompiler().Compile("SharedReadRejectionBeforeReaction", SemanticDocumentSet.Create([document], catalog));
            Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
            return compilation.Value!.Model;
        }
    }
}
#endif
