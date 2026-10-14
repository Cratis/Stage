// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_refusal_specifications(when_comparing_refusal_specifications.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_refusal_specifications.context>
{
    [Fact] void should_execute_both_branch_shapes() => fixture.Outcomes.Length.ShouldEqual(2);
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_pass_every_rendered_specification() => fixture.Outcomes.All(outcome => outcome.EndsWith("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldBeTrue();

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "Refusals";
        protected override string ProjectFile => "Refusals.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("inline", a_refusal_model.Compile())];
        Task Because() => Verify();
    }
}
#endif
