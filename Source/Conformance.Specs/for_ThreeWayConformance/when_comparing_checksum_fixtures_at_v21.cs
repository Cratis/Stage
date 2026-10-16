// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_checksum_fixtures_at_v21(when_comparing_checksum_fixtures_at_v21.context fixture) : IClassFixture<when_comparing_checksum_fixtures_at_v21.context>
{
    [Fact] void should_execute_every_valid_invalid_and_pinned_case_on_all_three_paths() => fixture.Outcomes.Count(outcome => outcome.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(given.checksum_fixtures_corpus.Count);
    [Fact] void should_have_no_three_way_differences() => Assert.True(fixture.Differences.IsEmpty, string.Join(Environment.NewLine, fixture.Differences));

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "ChecksumFixtures";
        protected override string ProjectFile => "ChecksumFixtures.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("checksum-fixtures/v21", given.checksum_fixtures_corpus.V21)];

        Task Because() => Verify();
    }
}
#endif
