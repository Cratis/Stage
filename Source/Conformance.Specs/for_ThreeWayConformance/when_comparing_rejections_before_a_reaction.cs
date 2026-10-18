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
/// Proves that a command rejected by its own validation never sets off the reaction listening to its event: the specification renders and passes
/// on the reference evaluator, the Stage executor and the rendered application.
/// </summary>
/// <param name="fixture">The three-way fixture.</param>
/// <param name="output">The test output.</param>
public class when_comparing_rejections_before_a_reaction(when_comparing_rejections_before_a_reaction.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_rejections_before_a_reaction.context>
{
    const int SpecificationCount = 2;

    [Fact] void should_compare_every_specification() => fixture.Outcomes.Length.ShouldEqual(SpecificationCount);
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_have_no_differences() => fixture.Differences.ShouldBeEmpty();
    [Fact] void should_pass_every_outcome_on_every_path() => fixture.Outcomes.Count(outcome => outcome.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(SpecificationCount);

    public class context : a_three_way_application
    {
        const string Source = """
            policy IsGardener
              require role "Gardener"
            module Plants
              feature Approval
                slice StateChange Placing
                  event PlantPlaced
                    id String
                    name String
                  command PlacePlant
                    id String identifier
                    name String
                    authorize IsGardener
                    validate
                      name not empty message "A plant needs a name."
                    produces PlantPlaced
                      for id
                      id = id
                      name = name
                  specification PlacingAnUnnamedPlant
                    given caller
                      authenticated
                      role "Gardener"
                    when PlacePlant
                      id = "plant"
                      name = ""
                    then error "A plant needs a name."
                  specification PlacingAsAVisitor
                    given caller
                      authenticated
                      role "Visitor"
                    when PlacePlant
                      id = "plant"
                      name = "Fern"
                    then denied
                slice StateChange Watering
                  event PlantWatered
                  command WaterPlant
                    id String identifier
                    produces PlantWatered
                      for id
                slice Automation WaterAfterPlacing
                  reaction WaterPlaced
                    when PlantPlaced
                      id
                      invokes WaterPlant
                        id = id
            """;

        protected override string ApplicationName => "RejectionsBeforeReaction";
        protected override string ProjectFile => "RejectionsBeforeReaction.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("source", Compile())];
        Task Because() => Verify();

        static ExecutableSemanticModel Compile()
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("RejectionsBeforeReaction"));
            var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", Source);
            var compilation = new SemanticModelCompiler().Compile("RejectionsBeforeReaction", SemanticDocumentSet.Create([document], catalog));
            Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
            return compilation.Value!.Model;
        }
    }
}
#endif
