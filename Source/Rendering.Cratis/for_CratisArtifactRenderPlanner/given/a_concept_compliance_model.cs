// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public static class a_concept_compliance_model
{
    public static string Source => ConceptComplianceCorpus.V13.SourceForms.Single(form => form.Name == "single").Documents.Single().Text;

    public static ExecutableSemanticModel Compile(CanonicalCorpusSourceForm form, bool marked = true)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
            catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, marked ? document.Text : Unmarked(document.Text)));
        var compilation = new SemanticModelCompiler().Compile(ConceptComplianceCorpus.V13.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));

        return SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(compilation.Value!.Model));
    }

    public static string Unmarked(string source) => source
        .Replace(" pii secret", string.Empty, StringComparison.Ordinal)
        .Replace(" pii\n", "\n", StringComparison.Ordinal)
        .Replace(" secret\n", "\n", StringComparison.Ordinal)
        .Replace("  pii special health\n", string.Empty, StringComparison.Ordinal)
        .Replace("  pii criminal\n", string.Empty, StringComparison.Ordinal)
        .Replace("  secret scope namespace\n", string.Empty, StringComparison.Ordinal);

    public const string Lookup = """
            slice StateView Lookup
              readmodel Information
                id InvoiceId
                health HealthInformation
                apiKey ApiKey
                note PersonalSecret
                status InvoiceStatus
              projection InformationProjection => Information
                from InformationRecorded key $eventSourceId
                  health = health
                  apiKey = key
                  note = note
                  status = status
              query ById => Information optional
                by id InvoiceId
        """;
}
