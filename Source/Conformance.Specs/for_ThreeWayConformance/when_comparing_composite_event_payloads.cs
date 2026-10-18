// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_composite_event_payloads(when_comparing_composite_event_payloads.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_composite_event_payloads.context>
{
    [Fact] void should_cover_all_four_event_paths() => fixture.Outcomes.Length.ShouldEqual(4);
    [Fact] void should_match_all_three_engines() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_execute_composite_facts() => Assert.All(fixture.Outcomes, outcome => Assert.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", outcome, StringComparison.Ordinal));

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "CompositeFacts";
        protected override string ProjectFile => "CompositeFacts.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("typed", when_running_composite_event_values.Compile().Model)];
        Task Because() => Verify();
    }
}
#endif
