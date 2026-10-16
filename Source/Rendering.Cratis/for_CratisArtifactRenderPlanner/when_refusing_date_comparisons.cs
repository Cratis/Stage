// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_date_comparisons : Specification
{
    [Theory]
    [InlineData("single")]
    [InlineData("folder")]
    [InlineData("reordered")]
    [InlineData("relocated")]
    public void should_refuse_the_v26_corpus_without_artifacts(string formName)
    {
        var corpus = DateComparisonsCorpus.V26;
        var form = corpus.SourceForms.Single(form => form.Name == formName);
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
            catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var compilation = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var model = compilation.Value!.Model;
        model.SemanticVersion.ShouldEqual(SemanticVersion.V26);
        Refuse(model, new(ArtifactRenderScopeKind.Application, model.Application.Id));
        foreach (var slice in SemanticVersionFeatures.Slices(model.Application))
            Refuse(model, new(ArtifactRenderScopeKind.Slice, slice.Id));
    }

    [Theory]
    [InlineData("Date", "require at >= from")]
    [InlineData("Date", "at >= \"2026-01-01\"")]
    [InlineData("Date", "production")]
    [InlineData("DateTime", "require at == from")]
    [InlineData("DateTime", "at != \"2026-01-01T00:00:00Z\"")]
    [InlineData("DateTime", "production")]
    public void should_name_concept_properties_in_all_three_positions(string primitive, string clause)
    {
        var source = $$"""
            concept Moment : {{primitive}}
            module Scheduling
              feature Windows
                slice StateChange Keep
                  command Keep
                    id String identifier
                    from Moment
                    at Moment
            {{(clause == "production" ? "" : $"        validate\n          {clause}")}}
            {{(clause == "production" ? "        produces when at >= from\n          Kept\n            for id" : "        produces Kept\n          for id")}}
                  event Kept
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Dates"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "app.play", source);
        var compilation = new SemanticModelCompiler().Compile("Dates", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var model = compilation.Value!.Model;
        Refuse(model, new(ArtifactRenderScopeKind.Application, model.Application.Id), "at");
        Refuse(model, new(ArtifactRenderScopeKind.Slice, SemanticVersionFeatures.Slices(model.Application).Single().Id), "at");
    }

    static void Refuse(ExecutableSemanticModel model, ArtifactRenderScope scope, string? property = null)
    {
        var plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model, []).Plan!, scope, new("Dates", "Dates"));
        plan.Artifacts.ShouldBeEmpty();
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-046" && (property is null || diagnostic.Message.Contains($"'{property}'", StringComparison.Ordinal)));
        plan.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == "STAGE-ESM-016");
    }
}
