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
/// Proves that a command with protected reads, a read-backed requirement and a validated request concept fails in the same order
/// on the reference evaluator, the Stage executor and the rendered application.
/// </summary>
/// <param name="fixture">The three-way fixture.</param>
/// <param name="output">The test output.</param>
public class when_comparing_protected_reads_with_validated_concepts(when_comparing_protected_reads_with_validated_concepts.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_protected_reads_with_validated_concepts.context>
{
    const int SpecificationCount = 6;

    [Fact] void should_compare_every_specification() => fixture.Outcomes.Length.ShouldEqual(SpecificationCount);
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_have_no_differences() => fixture.Differences.ShouldBeEmpty();
    [Fact] void should_pass_every_outcome_on_every_path() => fixture.Outcomes.Count(outcome => outcome.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(SpecificationCount);

    public class context : a_three_way_application
    {
        const string Source = """
            command reads protected
            concept Amount : Int
              validate
                > 0 message "An amount is at least one."
            module Plants
              feature Approval
                slice StateChange Preparing
                  event PlantPrepared
                    stock Int
                  command PreparePlant
                    id String identifier
                    stock Int
                    produces PlantPrepared
                      for id
                      stock = stock
                  event PlantTaken
                    amount Amount
                  command TakeFromPlant
                    id String identifier
                    amount Amount
                    reads PlantStock by id
                    validate
                      require PlantStock.stock > 0
                        message "The plant is out of stock."
                    produces PlantTaken
                      for id
                      amount = amount
                  specification TakingFromAStockedPlant
                    given PlantPrepared
                      for "plant"
                      stock = 3
                    when TakeFromPlant
                      id = "plant"
                      amount = 2
                    then PlantTaken
                      for "plant"
                      amount = 2
                  specification TakingFromAnEmptyPlant
                    given PlantPrepared
                      for "plant"
                      stock = 0
                    when TakeFromPlant
                      id = "plant"
                      amount = 2
                    then error "The plant is out of stock."
                  specification TakingFromAnUnknownPlant
                    when TakeFromPlant
                      id = "unknown"
                      amount = 2
                    then error "PlantStock 'unknown' does not exist."
                  specification TakingAnInvalidAmountFromAStockedPlant
                    given PlantPrepared
                      for "plant"
                      stock = 3
                    when TakeFromPlant
                      id = "plant"
                      amount = 0
                    then error "An amount is at least one."
                  specification TakingAnInvalidAmountFromAnEmptyPlant
                    given PlantPrepared
                      for "plant"
                      stock = 0
                    when TakeFromPlant
                      id = "plant"
                      amount = 0
                    then error "An amount is at least one."
                  specification TakingAnInvalidAmountFromAnUnknownPlant
                    when TakeFromPlant
                      id = "unknown"
                      amount = 0
                    then error "An amount is at least one."
                slice StateChange Removing
                  event PlantRemoved
                  command RemovePlant
                    id String identifier
                    produces PlantRemoved
                      for id
                slice StateView Stocks
                  readmodel PlantStock
                    id String key
                    stock Int
                  projection BuildStock => PlantStock
                    from PlantPrepared
                      id = $eventSourceId
                      stock = stock
                    remove with PlantRemoved
                  query StockById => PlantStock optional
                    by id String
            """;

        protected override string ApplicationName => "ReadsWithValidatedConcepts";
        protected override string ProjectFile => "ReadsWithValidatedConcepts.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("source", Compile())];
        Task Because() => Verify();

        static ExecutableSemanticModel Compile()
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("ReadsWithValidatedConcepts"));
            var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", Source);
            var compilation = new SemanticModelCompiler().Compile("ReadsWithValidatedConcepts", SemanticDocumentSet.Create([document], catalog));
            Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
            return compilation.Value!.Model;
        }
    }
}
#endif
