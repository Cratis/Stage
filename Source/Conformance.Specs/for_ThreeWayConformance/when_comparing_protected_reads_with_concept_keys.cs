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
/// Proves that a protected read keyed by an identifier concept named id (a Uuid concept, as in a typical application) is admitted by Chronicle's decision-read
/// guard and fails in the same order on the reference evaluator, the Stage executor and the rendered application.
/// </summary>
/// <param name="fixture">The three-way fixture.</param>
/// <param name="output">The test output.</param>
public class when_comparing_protected_reads_with_concept_keys(when_comparing_protected_reads_with_concept_keys.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_protected_reads_with_concept_keys.context>
{
    const int SpecificationCount = 3;

    [Fact] void should_compare_every_specification() => fixture.Outcomes.Length.ShouldEqual(SpecificationCount);
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_have_no_differences() => fixture.Differences.ShouldBeEmpty();
    [Fact] void should_pass_every_outcome_on_every_path() => fixture.Outcomes.Count(outcome => outcome.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(SpecificationCount);

    public class context : a_three_way_application
    {
        const string Source = """
            command reads protected
            concept PlantId : Uuid
            module Plants
              feature Approval
                slice StateChange Preparing
                  event PlantPrepared
                    ready Bool
                  command PreparePlant
                    plantId PlantId identifier
                    ready Bool
                    produces PlantPrepared
                      for plantId
                      ready = ready
                slice StateChange Approving
                  event PlantApproved
                  command ApprovePlant
                    plantId PlantId identifier
                    reads PlantReadiness by plantId
                    validate
                      require PlantReadiness.ready == true
                        message "Prepare the plant first."
                    produces PlantApproved
                      for plantId
                  specification ApprovingAPreparedPlant
                    given PlantPrepared
                      for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                      ready = true
                    when ApprovePlant
                      plantId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                    then PlantApproved
                      for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  specification ApprovingAnUnpreparedPlant
                    given PlantPrepared
                      for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                      ready = false
                    when ApprovePlant
                      plantId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                    then error "Prepare the plant first."
                  specification ApprovingAnUnknownPlant
                    when ApprovePlant
                      plantId = "4fa85f64-5717-4562-b3fc-2c963f66afa7"
                    then error "PlantReadiness '4fa85f64-5717-4562-b3fc-2c963f66afa7' does not exist."
                slice StateChange Withdrawing
                  event PlantWithdrawn
                  command WithdrawPlant
                    plantId PlantId identifier
                    produces PlantWithdrawn
                      for plantId
                slice StateView Readiness
                  readmodel PlantReadiness
                    id PlantId key
                    ready Bool
                  projection BuildReadiness => PlantReadiness
                    from PlantPrepared
                      id = $eventSourceId
                      ready = ready
                    remove with PlantWithdrawn
                  query ReadinessById => PlantReadiness optional
                    by id PlantId
            """;

        protected override string ApplicationName => "ReadsWithConceptKeys";
        protected override string ProjectFile => "ReadsWithConceptKeys.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("source", Compile())];
        Task Because() => Verify();

        static ExecutableSemanticModel Compile()
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("ReadsWithConceptKeys"));
            var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", Source);
            var compilation = new SemanticModelCompiler().Compile("ReadsWithConceptKeys", SemanticDocumentSet.Create([document], catalog));
            Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
            return compilation.Value!.Model;
        }
    }
}
#endif
