// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_numbered_enumerations : Specification
{
    ExecutableSemanticModel _model = null!;
    ArtifactRenderPlan _plan = null!;
    string Enumeration => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath == "Common/InvoiceStatus.cs").Bytes.AsSpan());

    void Establish()
    {
        var corpus = EnumMemberNumbersCorpus.V12;
        var form = corpus.SourceForms.Single(source => source.Name == "single");
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());

        // Stage renders one command per state-change slice; keep all three named member cases.
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, SingleCommandSource(document.Text)));
        var compilation = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
        compilation.Success.ShouldBeTrue();
        _model = SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(compilation.Value!.Model));
    }

    static string SingleCommandSource(string source) => source
        .Replace("      command Issue\n        id InvoiceId identifier\n        produces StatusRecorded\n          for id\n          status = \"issued\"\n", string.Empty, StringComparison.Ordinal)
        .Replace("        when Issue\n", "        when RecordStatus\n          status = \"issued\"\n", StringComparison.Ordinal);

    void Because() => _plan = invoice_model.Plan(_model);

    [Fact] void should_admit_v12() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V12);
    [Fact] void should_render_the_whole_application() => Assert.True(_plan.Success, string.Join('\n', _plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
    [Fact] void should_preserve_negative_identity() => Enumeration.ShouldContain("Draft = -1,");
    [Fact] void should_preserve_sparse_identity() => Enumeration.ShouldContain("Issued = 4,");
    [Fact] void should_preserve_the_last_identity() => Enumeration.ShouldContain("Cancelled = 90,");
}
