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

public class when_comparing_required_nested_checksums(when_comparing_required_nested_checksums.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_required_nested_checksums.context>
{
    [Fact] void should_cover_valid_and_invalid_checksums() => fixture.Outcomes.Length.ShouldEqual(2);
    [Fact] void should_match_all_three_engines() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_execute_nested_checksum_rules() => Assert.All(fixture.Outcomes, outcome => Assert.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", outcome, StringComparison.Ordinal));

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "NestedChecksums";
        protected override string ProjectFile => "NestedChecksums.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("typed", nested_checksum_cases.Compile(false))];
        Task Because() => Verify();
    }
}
#endif
