// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_comparing_date_comparisons(when_comparing_date_comparisons.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_date_comparisons.context>
{
    [Fact] void should_compare_all_six_specifications_in_all_four_source_forms() => fixture.Outcomes.Length.ShouldEqual(24);
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "DateComparisons";
        protected override string ProjectFile => "DateComparisons.csproj";
        protected override bool RequireStageExecution => true;
        protected override CanonicalCorpusVector? Baseline => DateComparisonsCorpus.V26;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => DateComparisonsCorpus.V26.SourceForms.Select(form => (form.Name, Compile(DateComparisonsCorpus.V26, form)));
        Task Because() => Verify();
    }
}
#endif
