// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Conformance.Specs.for_MatchEndOfInput;

public class when_comparing_the_canonical_source(when_comparing_the_canonical_source.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_the_canonical_source.context>
{
    [Fact] void should_cover_every_canonical_example() => fixture.Outcomes.Length.ShouldEqual(2);
    [Fact] void should_accept_the_whole_input_in_every_execution() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("AcceptingTheWholeInput: Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal));
    [Fact] void should_reject_a_trailing_newline_in_the_generated_application() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("RejectingATrailingNewline: Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal));
    [Fact]
    void should_match_all_three_paths()
    {
        foreach (var outcome in fixture.Outcomes) output.WriteLine(outcome);
        Assert.True(fixture.Differences.IsEmpty, string.Join(Environment.NewLine, fixture.Differences.Concat(fixture.Outcomes)));
    }

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "MatchEndOfInput";
        protected override string ProjectFile => "MatchEndOfInput.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("canonical", CompileSource())];

        Task Because() => Verify();

        static ExecutableSemanticModel CompileSource()
        {
            using var resource = typeof(RegisterProjectCorpus).Assembly.GetManifestResourceStream("Cratis.Screenplay.CanonicalCorpus.Corpus.MatchEndOfInput.source.play")!;
            using var reader = new StreamReader(resource);
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("MatchEndOfInput"));
            var document = SemanticSourceDocument.Create(catalog.ResolveDocument("source"), "source", "MatchEndOfInput.play", reader.ReadToEnd());
            var compilation = new SemanticModelCompiler().Compile("MatchEndOfInput", SemanticDocumentSet.Create([document], catalog));
            Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));

            return compilation.Value!.Model;
        }
    }
}
#endif
