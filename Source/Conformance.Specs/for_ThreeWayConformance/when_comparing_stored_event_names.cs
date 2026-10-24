// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_comparing_stored_event_names(when_comparing_stored_event_names.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_stored_event_names.context>
{
    [Fact] void should_compare_all_four_unmodified_source_forms() => fixture.Outcomes.Length.ShouldEqual(4);
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_preserve_mixed_event_names_in_every_form() => fixture.Outcomes.Count(outcome => outcome.EndsWith("MixedNames: Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(4);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "StoredEventNames";
        protected override string ProjectFile => "StoredEventNames.csproj";
        protected override CanonicalCorpusVector Baseline => StoredEventNamesCorpus.V16;
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => StoredEventNamesCorpus.SourceForms.Select(form => (form.Name, Compile(StoredEventNamesCorpus.V16, form)));
        Task Because() => Verify();
    }
}
#endif
