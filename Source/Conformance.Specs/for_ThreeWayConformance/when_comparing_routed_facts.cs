// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.given;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_comparing_routed_facts(when_comparing_routed_facts.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_routed_facts.context>
{
    [Fact] void should_run_the_rendered_routed_specification() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("Depositing: Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal));
    [Fact] void should_fail_a_different_stream_identity_on_every_path() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("WrongStream: Reference=Failed, Stage=Failed, Rendered=Failed", StringComparison.Ordinal));
    [Fact] void should_leave_an_omitted_route_as_a_wildcard() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("WildcardRoute: Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal));
    [Fact] void should_fail_an_unrouted_expectation_on_a_routed_fact() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("ExpectingUnrouted: Reference=Failed, Stage=Failed, Rendered=Failed", StringComparison.Ordinal));
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "Routed";
        protected override string ProjectFile => "Routed.csproj";
        protected override bool ExpectFailedGeneratedTest => true;
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("routes", Model())];
        Task Because() => Verify();

        static ExecutableSemanticModel Model()
        {
            var model = routed_specifications.Compile(routed_specifications.Source + """

                  specification WrongStream
                    when Deposit
                      accountId = "acc-1"
                      period = "2026-10"
                    then Deposited
                      stream Account.Transactions
                        streamId = "2026-11"
                  specification WildcardRoute
                    when Deposit
                      accountId = "acc-1"
                      period = "2026-10"
                    then Deposited
                  specification ExpectingUnrouted
                    when Deposit
                      accountId = "acc-1"
                      period = "2026-10"
                    then Deposited
            """);
            return routed_specifications.WithUnroutedExpectation(model, "ExpectingUnrouted");
        }
    }
}

public class when_comparing_routed_any_order_assignment(when_comparing_routed_any_order_assignment.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_routed_any_order_assignment.context>
{
    [Fact] void should_reassign_the_wildcard_match_on_every_path() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal));
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "Routed";
        protected override string ProjectFile => "Routed.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("assignment", routed_specifications.Assignment())];
        Task Because() => Verify();
    }
}

public class when_comparing_routed_composite_facts(when_comparing_routed_composite_facts.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_routed_composite_facts.context>
{
    [Fact] void should_format_seeded_and_expected_parts_like_the_command() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal));
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "Routed";
        protected override string ProjectFile => "Routed.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("composite", routed_specifications.Composite())];
        Task Because() => Verify();
    }
}

public class when_comparing_routed_composite_mismatches(when_comparing_routed_composite_mismatches.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_routed_composite_mismatches.context>
{
    [Fact] void should_fail_if_one_composite_part_differs() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("Reference=Failed, Stage=Failed, Rendered=Failed", StringComparison.Ordinal));
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "Routed";
        protected override string ProjectFile => "Routed.csproj";
        protected override bool ExpectFailedGeneratedTest => true;
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("wrong-composite", routed_specifications.Composite(wrongPart: true))];
        Task Because() => Verify();
    }
}
#endif
