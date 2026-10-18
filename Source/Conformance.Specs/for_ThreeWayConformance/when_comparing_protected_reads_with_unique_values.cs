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
/// Proves that a command whose event takes its constrained value from a protected read fails in the same order on the reference evaluator,
/// the Stage executor and the rendered application: a missing read, then the requirements, then the unique-value constraint.
/// </summary>
/// <param name="fixture">The three-way fixture.</param>
/// <param name="output">The test output.</param>
public class when_comparing_protected_reads_with_unique_values(when_comparing_protected_reads_with_unique_values.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_protected_reads_with_unique_values.context>
{
    const int SpecificationCount = 5;

    [Fact] void should_compare_every_specification() => fixture.Outcomes.Length.ShouldEqual(SpecificationCount);
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_have_no_differences() => fixture.Differences.ShouldBeEmpty();
    [Fact] void should_pass_every_outcome_on_every_path() => fixture.Outcomes.Count(outcome => outcome.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(SpecificationCount);

    public class context : a_three_way_application
    {
        const string Source = """
            command reads protected
            module Plants
              feature Naming
                slice StateChange Registering
                  event PlantRegistered
                    label String
                  command RegisterPlant
                    id String identifier
                    label String
                    produces PlantRegistered
                      for id
                      label = label
                  event LabelClaimed
                    label String
                  constraint OneClaimPerLabel
                    unique label on LabelClaimed
                  command ClaimLabel
                    id String identifier
                    lookup String
                    reads RegisteredPlant by lookup
                    validate
                      require RegisteredPlant.label != ""
                        message "The plant needs a label."
                    produces LabelClaimed
                      for id
                      label = RegisteredPlant.label
                  specification ClaimingALabel
                    given PlantRegistered
                      for "plant"
                      label = "fern"
                    when ClaimLabel
                      id = "claim"
                      lookup = "plant"
                    then LabelClaimed
                      for "claim"
                      label = "fern"
                  specification ClaimingAnUnlabelledPlant
                    given PlantRegistered
                      for "plant"
                      label = ""
                    when ClaimLabel
                      id = "claim"
                      lookup = "plant"
                    then error "The plant needs a label."
                  specification ClaimingAnUnknownPlant
                    when ClaimLabel
                      id = "claim"
                      lookup = "unknown"
                    then error "RegisteredPlant 'unknown' does not exist."
                  specification ClaimingATakenLabel
                    given PlantRegistered
                      for "plant"
                      label = "fern"
                    given LabelClaimed
                      for "earlier"
                      label = "fern"
                    when ClaimLabel
                      id = "claim"
                      lookup = "plant"
                    then error "Constraint 'OneClaimPerLabel' is violated: another event source already holds the constrained value."
                  specification ClaimingAnotherLabel
                    given PlantRegistered
                      for "plant"
                      label = "fern"
                    given LabelClaimed
                      for "earlier"
                      label = "moss"
                    when ClaimLabel
                      id = "claim"
                      lookup = "plant"
                    then LabelClaimed
                      for "claim"
                      label = "fern"
                slice StateChange Removing
                  event PlantRemoved
                  command RemovePlant
                    id String identifier
                    produces PlantRemoved
                      for id
                slice StateView Registry
                  readmodel RegisteredPlant
                    id String key
                    label String
                  projection BuildRegistry => RegisteredPlant
                    from PlantRegistered
                      id = $eventSourceId
                      label = label
                    remove with PlantRemoved
                  query PlantById => RegisteredPlant optional
                    by id String
            """;

        protected override string ApplicationName => "ReadsWithUniqueValues";
        protected override string ProjectFile => "ReadsWithUniqueValues.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("source", Compile())];
        Task Because() => Verify();

        static ExecutableSemanticModel Compile()
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("ReadsWithUniqueValues"));
            var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", Source);
            var compilation = new SemanticModelCompiler().Compile("ReadsWithUniqueValues", SemanticDocumentSet.Create([document], catalog));
            Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
            return compilation.Value!.Model;
        }
    }
}
#endif
