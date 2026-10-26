// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Xml.Linq;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_executing_a_generated_refusal_without_decision_reads(when_executing_a_generated_refusal_without_decision_reads.context fixture, ITestOutputHelper output) : IClassFixture<when_executing_a_generated_refusal_without_decision_reads.context>
{
    [Fact] void should_type_the_scenario_by_the_invoked_command() => fixture.GeneratedSpecification.ShouldContain("CommandScenario<Complete> _scenario = new();");
    [Fact] void should_not_use_an_untyped_scenario() => fixture.GeneratedSpecification.ShouldNotContain("CommandScenario<object>");
    [Fact] void should_not_use_the_legacy_event_scenario() => fixture.GeneratedSpecification.ShouldNotContain("EventScenario");
    [Fact] void should_execute_three_generated_tests() => fixture.GeneratedResults.Length.ShouldEqual(3);
    [Fact] void should_execute_the_settlement_count_and_exact_refusal_event_assertions() => fixture.GeneratedResults.Select(result => result.Name.Split('.')[^1]).ShouldContainOnly(["should_settle", "should_append_exactly_the_expected_events", "should_append_event_1"]);
    [Fact] void should_pass_every_generated_test_without_skipping() => Assert.All(fixture.GeneratedResults, result => Assert.Equal("Passed", result.Outcome));
    [Fact] void should_pass_the_refusal_expectation_on_all_three_paths() => fixture.Outcomes.ShouldContainOnly(["single-refusal/Completing: Reference=Passed, Stage=Passed, Rendered=Passed"]);
    [Fact] void should_preserve_three_way_parity() => ThreeWayOutcomes.Report(fixture, output);

    public class context : a_three_way_application
    {
        const string ModelSource = """
            module Work
              feature Processing
                slice Automation FollowUp
                  command Start
                    id String identifier
                    amount Decimal
                    produces Started
                      for id
                      id = id
                      amount = amount
                  command SeedHistory
                    id String identifier
                    produces Seeded
                      for id
                  command Complete
                    id String identifier
                    amount Decimal
                    validate
                      amount > 0 message "Positive amount required"
                    produces Completed
                      for id
                  event Started
                    id String
                    amount Decimal
                  event Seeded
                  event Completed
                  event Failed
                    reason String
                  reaction Worker
                    when Started
                      invokes Complete
                        id = id
                        amount = amount
                        on refused by validation
                          produces Failed
                            reason = $refusal.message
                  specification Completing
                    when append Started
                      for "one"
                      id = "one"
                      amount = 0
                    then Failed
                      for "one"
                      reason = "Positive amount required"
            """;

        protected override string ApplicationName => "Refusals";
        protected override string ProjectFile => "Refusals.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("single-refusal", a_refusal_model.Compile(ModelSource))];
        public string GeneratedSpecification { get; private set; } = string.Empty;
        public (string Name, string Outcome)[] GeneratedResults { get; private set; } = [];

        async Task Because()
        {
            // Verify removes the generated application, but retains its runtime evidence.
            GeneratedSpecification = ReadGeneratedFile(GeneratedFiles("when_completing.cs").Single());
            await Verify();
            XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
            GeneratedResults = [.. XDocument.Load(Path.Combine(_evidence.FullName, "parity.trx"))
                .Descendants(ns + "UnitTestResult")
                .Select(result => (result.Attribute("testName")!.Value, result.Attribute("outcome")!.Value))];
        }
    }
}
#endif
