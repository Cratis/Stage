// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rejecting_a_collection_query_comparison : Specification
{
    bool _supported;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("scopes"), "scopes", "Scopes.play", when_rendering_scoped_projections.ScopedSource);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success);
        var readModel = compilation.Value!.Model.Application.Modules.Single().Features.Single().Slices
            .Single(slice => slice.Kind == SemanticSliceKind.StateView).ReadModels.Single(model => model.Name == "ProjectSummary");
        var collection = readModel.Properties.Single(property => property.Name == "notes");
        _supported = SemanticSpecificationAdmission.CanCompareQueryResult([new(collection.Id, SemanticValue.Array([]))], readModel.Properties);
    }

    [Fact] void should_not_use_reference_equality_for_the_query_result() => _supported.ShouldBeFalse();

    [Fact] void should_reject_a_modeled_collection_query_result_without_emitting_artifacts()
    {
        var source = RegisterProjectCorpus.LegacyV1.SourceForms.Single(form => form.Name == "single").Documents.Single().Text
            .Replace("        name ProjectName\n        validate", "        name ProjectName\n        labels String[]\n        validate", StringComparison.Ordinal)
            .Replace("      event ProjectRegistered\n        projectId ProjectId\n        name ProjectName", "      event ProjectRegistered\n        projectId ProjectId\n        name ProjectName\n        labels String[]", StringComparison.Ordinal)
            .Replace("      readmodel ProjectSummary\n        projectId ProjectId\n        name ProjectName", "      readmodel ProjectSummary\n        projectId ProjectId\n        name ProjectName\n        labels String[]", StringComparison.Ordinal);
        source = string.Join('\n', source.Split('\n').Select(line =>
            line.TrimStart() == "name = name" || line.TrimStart() == "name = \"Screenplay\""
                ? $"{line}\n{new string(' ', line.Length - line.TrimStart().Length)}labels = {(line.Contains('"') ? "[\"a\"]" : "labels")}" : line));
        source = source.Replace("          name = \"\"\n", "          name = \"\"\n          labels = [\"a\"]\n", StringComparison.Ordinal);
        var plan = invoice_model.Plan(invoice_model.Compile(source));
        plan.Diagnostics.Where(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).Select(diagnostic => diagnostic.Code)
            .ShouldContainOnly(["STAGE-ESM-011"]);
        plan.Artifacts.ShouldBeEmpty();
    }
}
