// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_PortableMatches.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_PortableMatches;

public class when_comparing_the_canonical_source(when_comparing_the_canonical_source.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_the_canonical_source.context>
{
    [Fact] void should_cover_the_canonical_anchors_and_every_command_and_concept_case() => fixture.Outcomes.Length.ShouldEqual(2 + (portable_match_cases.Cases.Length * 2));
    [Fact] void should_agree_on_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "PortableMatches";
        protected override string ProjectFile => "PortableMatches.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("canonical-and-portable", portable_match_cases.Compile())];
        Task Because() => Verify();
    }
}
#endif
