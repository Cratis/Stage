// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Xml.Linq;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionSpecificationRenderer.given;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_executing_generated_multiple_refusal_invocations(when_executing_generated_multiple_refusal_invocations.context fixture, ITestOutputHelper output) : IClassFixture<when_executing_generated_multiple_refusal_invocations.context>
{
    [Fact] void should_enable_decision_reads_immediately_on_the_untyped_scenario() => fixture.GeneratedSpecification.ShouldContain("CommandScenario<object> _scenario = new CommandScenario<object>().UseDecisionReads();");
    [Fact] void should_import_the_decision_reads_extension_namespace() => fixture.GeneratedSpecification.ShouldContain("using Cratis.Arc.Chronicle.Testing.Commands;");
    [Fact] void should_enable_decision_reads_before_seeding_history() => ShouldPrecede(".UseDecisionReads()", "_scenario.EventLog");
    [Fact] void should_enable_decision_reads_before_executing_commands() => ShouldPrecede(".UseDecisionReads()", "_scenario.Execute");
    [Fact] void should_check_the_seeded_history_append() => fixture.GeneratedSpecification.ShouldContain("(await _scenario.EventLog.Append(_id, new Seeded())).IsSuccess.ShouldBeTrue();");
    [Fact] void should_seed_history_before_appending_the_trigger() => ShouldPrecede("new Seeded()", "(await _scenario.EventLog.Append(_id, @event)).IsSuccess.ShouldBeTrue();");
    [Fact] void should_capture_the_initial_count_after_the_trigger() => ShouldPrecede("(await _scenario.EventLog.Append(_id, @event)).IsSuccess.ShouldBeTrue();", "var initial = before.Count;");
    [Fact] void should_capture_the_initial_count_before_executing_the_reaction() => ShouldPrecede("var initial = before.Count;", "var result = await executor.Execute(");
    [Fact] void should_exclude_seeded_history_and_the_trigger_from_appended_facts() => fixture.GeneratedSpecification.ShouldContain("_facts = [.. persisted.Skip(initial)];");
    [Fact] void should_execute_the_untyped_commands() => fixture.GeneratedSpecification.ShouldContain("_scenario.Execute(call.ArgAt<object>(0))");
    [Fact] void should_not_use_the_legacy_event_scenario() => fixture.GeneratedSpecification.ShouldNotContain("EventScenario");
    [Fact] void should_preserve_the_authored_source() => fixture.GeneratedSpecification.ShouldContain("static readonly string _id = \"one\";");
    [Fact] void should_expect_exactly_two_appended_events() => fixture.GeneratedSpecification.ShouldContain("_facts.Length.ShouldEqual(2);");
    [Fact] void should_expect_completed_first_from_the_authored_source() => fixture.GeneratedSpecification.ShouldContain("(_facts[0].Content is Completed @event && _facts[0].Context.EventSourceId == _id).ShouldBeTrue();");
    [Fact] void should_expect_followed_second_from_the_authored_source() => fixture.GeneratedSpecification.ShouldContain("(_facts[1].Content is Followed @event && _facts[1].Context.EventSourceId == _id).ShouldBeTrue();");
    [Fact] void should_execute_four_generated_tests() => fixture.GeneratedResults.Length.ShouldEqual(4);
    [Fact] void should_execute_the_settlement_count_and_both_ordered_event_assertions() => fixture.GeneratedResults.Select(result => result.Name.Split('.')[^1]).ShouldContainOnly(["should_settle", "should_append_exactly_the_expected_events", "should_append_event_1", "should_append_event_2"]);
    [Fact] void should_pass_every_generated_test_without_skipping() => Assert.All(fixture.GeneratedResults, result => Assert.Equal("Passed", result.Outcome));
    [Fact] void should_pass_the_successful_continuation_on_all_three_paths() => fixture.Outcomes.ShouldContainOnly(["multiple-refusal/Completing: Reference=Passed, Stage=Passed, Rendered=Passed"]);
    [Fact] void should_preserve_three_way_parity() => ThreeWayOutcomes.Report(fixture, output);

    void ShouldPrecede(string earlier, string later)
    {
        var first = fixture.GeneratedSpecification.IndexOf(earlier, StringComparison.Ordinal);
        first.ShouldBeGreaterThanOrEqual(0);
        fixture.GeneratedSpecification.IndexOf(later, StringComparison.Ordinal).ShouldBeGreaterThan(first);
    }

    public class context : a_three_way_application
    {
        // Preserve the authored successful continuation through a reaction with a refusal branch.
        static readonly string ModelSource = a_refusal_specification.RefusalSource
            .Replace("      event Started", "      command FollowUp\n        id String identifier\n        produces Followed\n          for id\n      event Followed\n      event Started", StringComparison.Ordinal)
            .Replace("      specification Completing", "          invokes FollowUp\n            id = id\n      specification Completing\n        given Seeded\n          for \"one\"", StringComparison.Ordinal)
            .Replace("        then Completed\n          for \"one\"", "        then Completed\n          for \"one\"\n        then Followed\n          for \"one\"", StringComparison.Ordinal);

        protected override string ApplicationName => "Refusals";
        protected override string ProjectFile => "Refusals.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("multiple-refusal", a_refusal_model.Compile(ModelSource))];
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
