// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_comparing_uuid_equality(when_comparing_uuid_equality.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_uuid_equality.context>
{
    [Fact] void should_compare_all_six_specifications_in_all_four_source_forms() => fixture.Outcomes.Length.ShouldEqual(24);
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "UuidComparisons";
        protected override string ProjectFile => "UuidComparisons.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => UuidComparisonsCorpus.V28.SourceForms.Select(form => (form.Name, Compile(form)));
        Task Because() => Verify();

        static ExecutableSemanticModel Compile(CanonicalCorpusSourceForm form)
        {
            var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
            var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
            var compilation = new SemanticModelCompiler().Compile("UuidComparisons", SemanticDocumentSet.Create([.. documents], catalog));
            Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
            compilation.Value.Model.LanguageVersion.ShouldEqual(LanguageVersion.V28);
            compilation.Value.Model.SemanticVersion.ShouldEqual(SemanticVersion.V28);
            return compilation.Value.Model;
        }
    }
}
#endif
