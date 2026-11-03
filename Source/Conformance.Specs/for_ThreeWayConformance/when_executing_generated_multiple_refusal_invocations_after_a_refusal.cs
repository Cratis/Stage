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

public class when_executing_generated_multiple_refusal_invocations_after_a_refusal(when_executing_generated_multiple_refusal_invocations_after_a_refusal.context fixture, ITestOutputHelper output) : IClassFixture<when_executing_generated_multiple_refusal_invocations_after_a_refusal.context>
{
    [Fact] void should_enable_decision_reads_immediately_on_the_untyped_scenario() => fixture.GeneratedSpecification.ShouldContain("CommandScenario<object> _scenario = new CommandScenario<object>().UseDecisionReads();");
    [Fact] void should_import_the_decision_reads_extension_namespace() => fixture.GeneratedSpecification.ShouldContain("using Cratis.Arc.Chronicle.Testing.Commands;");
    [Fact] void should_enable_decision_reads_before_appending_the_trigger() => ShouldPrecede(".UseDecisionReads()", "_scenario.EventLog");
    [Fact] void should_enable_decision_reads_before_executing_commands() => ShouldPrecede(".UseDecisionReads()", "_scenario.Execute");
    [Fact] void should_capture_the_initial_count_after_appending_the_trigger() => ShouldPrecede("@event)).IsSuccess.ShouldBeTrue();", "var initial = before.Count;");
    [Fact] void should_capture_the_initial_count_before_executing_the_reaction() => ShouldPrecede("var initial = before.Count;", "var result = await executor.Execute(");
    [Fact] void should_exclude_the_trigger_from_appended_facts() => fixture.GeneratedSpecification.ShouldContain("_facts = [.. persisted.Skip(initial)];");
    [Fact] void should_execute_the_untyped_commands() => fixture.GeneratedSpecification.ShouldContain("_scenario.Execute(call.ArgAt<object>(0))");
    [Fact] void should_not_use_the_legacy_event_scenario() => fixture.GeneratedSpecification.ShouldNotContain("EventScenario");
    [Fact] void should_assert_successful_settlement() => fixture.GeneratedSpecification.ShouldContain("void should_settle() => _settled.ShouldBeTrue();");
    [Fact] void should_assert_no_completed_or_followed_facts_are_appended() => fixture.GeneratedSpecification.ShouldContain("void should_append_exactly_the_expected_events() => _facts.Length.ShouldEqual(0);");
    [Fact] void should_execute_two_generated_tests() => fixture.GeneratedResults.Length.ShouldEqual(2);
    [Fact] void should_execute_the_settlement_and_no_appended_events_assertions() => fixture.GeneratedResults.Select(result => result.Name.Split('.')[^1]).ShouldContainOnly(["should_settle", "should_append_exactly_the_expected_events"]);
    [Fact] void should_pass_every_generated_test_without_skipping() => Assert.All(fixture.GeneratedResults, result => Assert.Equal("Passed", result.Outcome));
    [Fact] void should_pass_the_runtime_assertion_of_no_completed_or_followed_facts() => fixture.GeneratedResults.Single(result => result.Name.EndsWith(".should_append_exactly_the_expected_events", StringComparison.Ordinal)).Outcome.ShouldEqual("Passed");
    [Fact] void should_pass_the_first_matching_refusal_branch_on_all_three_paths() => fixture.Outcomes.ShouldContainOnly(["multiple-refusal-after-refusal/Exercising: Reference=Passed, Stage=Passed, Rendered=Passed"]);
    [Fact] void should_preserve_three_way_parity() => ThreeWayOutcomes.Report(fixture, output);

    void ShouldPrecede(string earlier, string later)
    {
        var first = fixture.GeneratedSpecification.IndexOf(earlier, StringComparison.Ordinal);
        first.ShouldBeGreaterThanOrEqual(0);
        fixture.GeneratedSpecification.IndexOf(later, StringComparison.Ordinal).ShouldBeGreaterThan(first);
    }

    public class context : a_three_way_application
    {
        // Expanded from a_reaction_plan with ReactionScenario defaults and the validation selector.
        // Both invocations and both matching branches are retained: neither Completed nor Followed may be appended.
        const string ModelSource = """

            concept Token : Uuid

            module Work
              feature Items
                slice Automation Process
                  command Begin
                    item String identifier
                    amount Decimal
                    produces Started
                      for item
                      item = item
                      amount = amount
                  command Complete
                    item String identifier

                    validate
                      item not empty message "required"
                    produces Completed
                      for item
                  event Started
                    item String
                    amount Decimal
                  event Completed
                  event Followed
                    item String
                  event Noted
                    notedAt DateTime
                  readmodel Marker
                    item String key
                  projection Markers => Marker
                    from Completed
                      item = $eventSourceId

                  reaction Worker
                    when Started
                      invokes Complete
                        item = ""
                        on refused by validation
                          acknowledge
                        on refused by validation
                          produces Followed
                            item = "wrong branch"
                      invokes Complete
                        item = "one"
                  specification Exercising

                    when append Started
                      for "one"
                      item = "one"
                      amount = 1
                    then no events
            """;

        protected override string ApplicationName => "Refusals";
        protected override string ProjectFile => "Refusals.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("multiple-refusal-after-refusal", a_refusal_model.Compile(ModelSource))];
        public string GeneratedSpecification { get; private set; } = string.Empty;
        public (string Name, string Outcome)[] GeneratedResults { get; private set; } = [];

        async Task Because()
        {
            // Verify removes the generated application, but retains its runtime evidence.
            GeneratedSpecification = ReadGeneratedFile(GeneratedFiles("when_exercising.cs").Single());
            await Verify();
            XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
            GeneratedResults = [.. XDocument.Load(Path.Combine(_evidence.FullName, "parity.trx"))
                .Descendants(ns + "UnitTestResult")
                .Select(result => (result.Attribute("testName")!.Value, result.Attribute("outcome")!.Value))];
        }
    }
}
#endif
