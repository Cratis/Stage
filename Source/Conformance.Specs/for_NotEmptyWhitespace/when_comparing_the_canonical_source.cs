// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Conformance.Specs.for_NotEmptyWhitespace;

public class when_comparing_the_canonical_source(when_comparing_the_canonical_source.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_the_canonical_source.context>
{
    [Fact] void should_cover_every_canonical_example() => fixture.Outcomes.Length.ShouldEqual(7);
    [Fact] void should_reject_whitespace_on_a_concept() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("RejectingWhitespaceOnAConcept: Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal));
    [Fact] void should_reject_next_line_on_a_concept() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("RejectingNextLineOnAConcept: Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal));
    [Fact] void should_accept_non_whitespace_on_a_concept() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("AcceptingNonWhitespaceOnAConcept: Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal));
    [Fact] void should_accept_a_byte_order_mark_on_a_concept() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("AcceptingByteOrderMarkOnAConcept: Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal));
    [Fact] void should_reject_whitespace_on_a_property() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("RejectingWhitespaceOnAProperty: Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal));
    [Fact] void should_reject_next_line_on_a_property() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("RejectingNextLineOnAProperty: Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal));
    [Fact] void should_accept_a_byte_order_mark_on_a_property() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("AcceptingByteOrderMarkOnAProperty: Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal));
    [Fact]
    void should_match_all_three_paths()
    {
        foreach (var outcome in fixture.Outcomes) output.WriteLine(outcome);
        Assert.True(fixture.Differences.IsEmpty, string.Join(Environment.NewLine, fixture.Differences.Concat(fixture.Outcomes)));
    }

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "NotEmptyWhitespace";
        protected override string ProjectFile => "NotEmptyWhitespace.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("canonical", CompileSource())];

        Task Because() => Verify();

        static ExecutableSemanticModel CompileSource()
        {
            using var resource = typeof(RegisterProjectCorpus).Assembly.GetManifestResourceStream("Cratis.Screenplay.CanonicalCorpus.Corpus.NotEmptyWhitespace.source.play")!;
            using var reader = new StreamReader(resource);
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("NotEmptyWhitespace"));
            var document = SemanticSourceDocument.Create(catalog.ResolveDocument("source"), "source", "NotEmptyWhitespace.play", reader.ReadToEnd());
            var compilation = new SemanticModelCompiler().Compile("NotEmptyWhitespace", SemanticDocumentSet.Create([document], catalog));
            Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));

            return compilation.Value!.Model;
        }
    }
}
#endif
