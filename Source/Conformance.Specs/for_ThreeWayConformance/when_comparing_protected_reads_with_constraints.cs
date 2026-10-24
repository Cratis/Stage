// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Proves that a command with protected reads which appends an event under an append-time constraint fails in the same order
/// on the reference evaluator, the Stage executor and the rendered application: a missing read, then the requirements, then the constraint.
/// </summary>
/// <param name="fixture">The three-way fixture.</param>
/// <param name="output">The test output.</param>
public class when_comparing_protected_reads_with_constraints(when_comparing_protected_reads_with_constraints.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_protected_reads_with_constraints.context>
{
    const int SpecificationCount = 7;

    [Fact] void should_compare_every_specification() => fixture.Outcomes.Length.ShouldEqual(SpecificationCount);
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_have_no_differences() => fixture.Differences.ShouldBeEmpty();
    [Fact] void should_pass_every_outcome_on_every_path() => fixture.Outcomes.Count(outcome => outcome.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(SpecificationCount);

    public class context : a_three_way_application
    {
        const string Source = """
            command reads protected
            module Plants
              feature Approval
                slice StateChange Preparing
                  event PlantPrepared
                    ready Bool
                  command PreparePlant
                    id String identifier
                    ready Bool
                    produces PlantPrepared
                      for id
                      ready = ready
                slice StateChange Approving
                  event PlantApproved
                  command ApprovePlant
                    id String identifier
                    reads PlantReadiness by id
                    validate
                      require PlantReadiness.ready == true
                        message "Prepare the plant first."
                    produces PlantApproved
                      for id
                  constraint OneApprovalPerPlant
                    unique event PlantApproved
                    released by PlantWithdrawn
                    message "A plant is approved once until it is withdrawn."
                  specification ApprovingAPreparedPlant
                    given PlantPrepared
                      for "plant"
                      ready = true
                    when ApprovePlant
                      id = "plant"
                    then PlantApproved
                      for "plant"
                  specification ApprovingAnUnpreparedPlant
                    given PlantPrepared
                      for "plant"
                      ready = false
                    when ApprovePlant
                      id = "plant"
                    then error "Prepare the plant first."
                  specification ApprovingAnUnknownPlant
                    when ApprovePlant
                      id = "unknown"
                    then error "PlantReadiness 'unknown' does not exist."
                  specification ApprovingTwice
                    given PlantPrepared
                      for "plant"
                      ready = true
                    given PlantApproved
                      for "plant"
                    when ApprovePlant
                      id = "plant"
                    then error "A plant is approved once until it is withdrawn."
                  specification ApprovingAfterAWithdrawal
                    given PlantPrepared
                      for "plant"
                      ready = true
                    given PlantApproved
                      for "plant"
                    given PlantWithdrawn
                      for "plant"
                    given PlantPrepared
                      for "plant"
                      ready = true
                    when ApprovePlant
                      id = "plant"
                    then PlantApproved
                      for "plant"
                  specification AnUnpreparedPlantThatWasApproved
                    given PlantPrepared
                      for "plant"
                      ready = false
                    given PlantApproved
                      for "plant"
                    when ApprovePlant
                      id = "plant"
                    then error "Prepare the plant first."
                  specification ApprovingAnotherPlantIsIndependent
                    given PlantPrepared
                      for "plant"
                      ready = true
                    given PlantPrepared
                      for "other"
                      ready = true
                    given PlantApproved
                      for "plant"
                    when ApprovePlant
                      id = "other"
                    then PlantApproved
                      for "other"
                slice StateChange Withdrawing
                  event PlantWithdrawn
                  command WithdrawPlant
                    id String identifier
                    produces PlantWithdrawn
                      for id
                slice StateView Readiness
                  readmodel PlantReadiness
                    id String key
                    ready Bool
                  projection BuildReadiness => PlantReadiness
                    from PlantPrepared
                      id = $eventSourceId
                      ready = ready
                    remove with PlantWithdrawn
                  query ReadinessById => PlantReadiness optional
                    by id String
            """;

        protected override string ApplicationName => "ReadsWithConstraints";
        protected override string ProjectFile => "ReadsWithConstraints.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("source", Compile())];
        Task Because() => Verify();

        static ExecutableSemanticModel Compile()
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("ReadsWithConstraints"));
            var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", Source);
            var compilation = new SemanticModelCompiler().Compile("ReadsWithConstraints", SemanticDocumentSet.Create([document], catalog));
            Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
            return compilation.Value.Model;
        }
    }
}
#endif
