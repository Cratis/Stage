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

public class when_comparing_routed_generated_values(when_comparing_routed_generated_values.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_routed_generated_values.context>
{
    [Fact] void should_execute_all_eleven_cases_on_every_path() => fixture.Outcomes.Count(outcome => outcome.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(11);
    [Fact] void should_reject_reuse_across_streams_on_every_path() => fixture.Outcomes.Count(outcome => outcome.Contains("CrossStreamReuse: Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(4);
    [Fact] void should_reject_empty_routes_without_appending_events() => fixture.Outcomes.Count(outcome => outcome.Contains("EmptyRoute: Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "RoutedGeneration";
        protected override string ProjectFile => "RoutedGeneration.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("routes", routed_generated_value_cases.Compile())];

        Task Because()
        {
            AddGeneratedSpecification("RoutedFreshness.cs", routed_generated_freshness_probe.Source);
            return Verify();
        }
    }
}
#endif
