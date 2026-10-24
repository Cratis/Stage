// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_routed_responses(when_comparing_routed_responses.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_routed_responses.context>
{
    [Fact] void should_execute_all_ten_response_cases() => fixture.Outcomes.Length.ShouldEqual(10);
    [Fact] void should_preserve_scalar_and_named_responses_on_every_path() => fixture.Outcomes.Count(outcome => outcome.Contains("Correct: Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(5);
    [Fact] void should_fail_every_wrong_response_on_every_path() => fixture.Outcomes.Count(outcome => outcome.Contains("Wrong: Reference=Failed, Stage=Failed, Rendered=Failed", StringComparison.Ordinal)).ShouldEqual(5);
    [Fact] void should_return_a_response_even_without_selected_events() => fixture.Outcomes.Count(outcome => outcome.Contains("ConditionalSkippedCorrect: Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "RoutedResponses";
        protected override string ProjectFile => "RoutedResponses.csproj";
        protected override bool RequireStageExecution => true;
        protected override bool ExpectFailedGeneratedTest => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("responses", routed_response_cases.Compile())];

        Task Because() => Verify();
    }
}
#endif
