// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_comparing_optional_date_concepts(when_comparing_optional_date_concepts.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_optional_date_concepts.context>
{
    [Fact] void should_compare_all_five_optional_nominal_cases() => fixture.Outcomes.Length.ShouldEqual(5);
    [Fact] void should_match_reference_arc_and_chronicle() => ThreeWayOutcomes.Report(fixture, output);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "NominalDates";
        protected override string ProjectFile => "NominalDates.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("typed", optional_date_concept_cases.Compile())];
        Task Because() => Verify();
    }
}
#endif
