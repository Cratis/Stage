// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Proves that a literal input breaking a concept rule of the triggering command is rejected while binding, before any event, even when the command
/// a reaction invokes carries the same concept (and so the same message): the specification renders and passes on the reference evaluator, the
/// Stage executor and the rendered application.
/// </summary>
/// <param name="fixture">The three-way fixture.</param>
/// <param name="output">The test output.</param>
public class when_comparing_concept_rejections_before_a_reaction(when_comparing_concept_rejections_before_a_reaction.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_concept_rejections_before_a_reaction.context>
{
    const int SpecificationCount = 1;

    [Fact] void should_compare_every_specification() => fixture.Outcomes.Length.ShouldEqual(SpecificationCount);
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_have_no_differences() => fixture.Differences.ShouldBeEmpty();
    [Fact] void should_pass_every_outcome_on_every_path() => fixture.Outcomes.Count(outcome => outcome.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(SpecificationCount);

    public class context : a_three_way_application
    {
        const string Source = """
            concept Quantity : Int
              validate
                > 0  message "A quantity is at least one"
            module Stock
              feature Receiving
                slice StateChange Requesting
                  event StockRequested
                    id String
                    quantity Quantity
                  command RequestStock
                    id String identifier
                    quantity Quantity
                    produces StockRequested
                      for id
                      id = id
                      quantity = quantity
                  specification ARequestOfZeroIsRejected
                    when RequestStock
                      id = "sku"
                      quantity = 0
                    then error "A quantity is at least one"
                slice StateChange Reserving
                  event StockReserved
                    id String
                    quantity Quantity
                  command ReserveStock
                    id String identifier
                    quantity Quantity
                    produces StockReserved
                      for id
                      id = id
                      quantity = quantity
                slice Automation ReserveAfterRequest
                  reaction ReserveRequested
                    when StockRequested
                      id
                      quantity
                      invokes ReserveStock
                        id = id
                        quantity = quantity
            """;

        protected override string ApplicationName => "ConceptRejectionsBeforeReaction";
        protected override string ProjectFile => "ConceptRejectionsBeforeReaction.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("source", Compile())];
        Task Because() => Verify();

        static ExecutableSemanticModel Compile()
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("ConceptRejectionsBeforeReaction"));
            var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", Source);
            var compilation = new SemanticModelCompiler().Compile("ConceptRejectionsBeforeReaction", SemanticDocumentSet.Create([document], catalog));
            Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
            return compilation.Value.Model;
        }
    }
}
#endif
