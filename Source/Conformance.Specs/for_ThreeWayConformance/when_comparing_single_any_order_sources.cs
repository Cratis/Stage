// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.given;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_comparing_single_any_order_sources(when_comparing_single_any_order_sources.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_single_any_order_sources.context>
{
    [Fact] void should_pass_the_correct_source_and_route() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("Depositing: Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal));
    [Fact] void should_fail_a_wrong_then_source_despite_a_correct_action_source() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("WrongThenSource: Reference=Failed, Stage=Failed, Rendered=Failed", StringComparison.Ordinal));
    [Fact] void should_fail_a_wrong_action_source_despite_a_correct_then_source() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("WrongActionSource: Reference=Failed, Stage=Failed, Rendered=Failed", StringComparison.Ordinal));
    [Fact] void should_fail_a_wrong_route_despite_correct_sources() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("WrongRoute: Reference=Failed, Stage=Failed, Rendered=Failed", StringComparison.Ordinal));
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "Routed";
        protected override string ProjectFile => "Routed.csproj";
        protected override bool ExpectFailedGeneratedTest => true;
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("single-any-order", Model())];
        Task Because() => Verify();

        static ExecutableSemanticModel Model()
        {
            var model = routed_specifications.Compile(routed_specifications.Source + "\n" + """
                      specification WrongThenSource
                        when Deposit
                          accountId = "acc-1"
                          period = "2026-10"
                        then Deposited
                          stream Account.Transactions
                            streamId = "2026-10"
                      specification WrongActionSource
                        when Deposit
                          accountId = "acc-1"
                          period = "2026-10"
                        then Deposited
                          stream Account.Transactions
                            streamId = "2026-10"
                      specification WrongRoute
                        when Deposit
                          accountId = "acc-1"
                          period = "2026-10"
                        then Deposited
                          stream Account.Transactions
                            streamId = "2026-11"
                """);
            var module = model.Application.Modules.Single();
            var feature = module.Features.Single();
            var slice = feature.Slices.Single();
            var type = slice.Commands.Single().Properties[0].Type;
            var changed = slice with
            {
                Specifications = [.. slice.Specifications.Select(specification => specification with
                {
                    ThenEventsInAnyOrder = true,
                    When = specification.When! with { EventSource = new(type, SemanticValue.Text(specification.Name == "WrongActionSource" ? "acc-2" : "acc-1")) },
                    ThenEvents = [specification.ThenEvents[0] with { EventSource = new(type, SemanticValue.Text(specification.Name == "WrongThenSource" ? "acc-2" : "acc-1")) }]
                })]
            };

            return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
            {
                Modules = [module with { Features = [feature with { Slices = [changed] }] }]
            });
        }
    }
}
#endif
