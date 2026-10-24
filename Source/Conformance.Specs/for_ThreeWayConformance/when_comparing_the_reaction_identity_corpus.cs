// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_comparing_the_reaction_identity_corpus(when_comparing_the_reaction_identity_corpus.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_the_reaction_identity_corpus.context>
{
    [Fact] void should_compare_both_specifications_in_all_four_unmodified_source_forms() => fixture.Outcomes.Length.ShouldEqual(8);
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_allow_the_declared_system_identity_in_every_form() => fixture.Outcomes.Count(outcome => outcome.EndsWith("DeclaredIdentityAllows: Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(4);
    [Fact] void should_deny_the_unmarked_reaction_despite_the_fixture_caller_in_every_form() => fixture.Outcomes.Count(outcome => outcome.EndsWith("NoIdentityDenies: Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(4);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "ReactionIdentity";
        protected override string ProjectFile => "ReactionIdentity.csproj";
        protected override CanonicalCorpusVector Baseline => ReactionIdentityCorpus.V10;
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => ReactionIdentityCorpus.SourceForms.Select(form => (form.Name, Compile(ReactionIdentityCorpus.V10, form)));
        Task Because() => Verify();
    }
}
#endif
