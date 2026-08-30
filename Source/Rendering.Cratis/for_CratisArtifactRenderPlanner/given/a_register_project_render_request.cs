// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public class a_register_project_render_request : Specification
{
    /// <summary>
    /// The frozen RegisterProject conformance corpus (Cratis/Screenplay#167, Cratis/Screenplay#173) - the single
    /// reviewed source of truth for this fixture's semantic baseline, loaded through the real serializers rather
    /// than a copied inline string or a repository-relative file.
    /// </summary>
    protected static readonly CanonicalCorpusVector Corpus = RegisterProjectCorpus.LegacyV1;

    /// <summary>
    /// The single-document physical source form's exact text, read from the corpus itself (never hand-copied) so
    /// specs that need a deliberate textual variant - e.g. injecting an additional construct - still start from
    /// the one reviewed source of truth.
    /// </summary>
    protected static readonly string Source = Corpus.SourceForms.Single(_ => _.Name == "single").Documents.Single().Text;

    protected CratisArtifactRenderPlanner _planner = null!;
    protected ArtifactRenderRequest _request = null!;
    protected ExecutableSemanticModel _model = null!;
    protected SemanticExecutionPlan _executionPlan = null!;
    protected CratisRenderingOptions _options = null!;
    protected SemanticModule _module = null!;
    protected SemanticFeature _feature = null!;
    protected SemanticSlice _registerProject = null!;
    protected SemanticSlice _projectLookup = null!;

    void Establish()
    {
        var singleForm = Corpus.SourceForms.Single(_ => _.Name == "single");
        _model = Compile(singleForm).Model;
        _executionPlan = SemanticExecutionPlan.Compile(_model).Plan!;
        _options = new("Projects", "Projects");
        var profile = CratisRendering.CreateProfile(_model.Application.Name, _options);

        _module = _model.Application.Modules.Single();
        _feature = _module.Features.Single();
        _registerProject = _feature.Slices.Single(_ => _.Kind == SemanticSliceKind.StateChange);
        _projectLookup = _feature.Slices.Single(_ => _.Kind == SemanticSliceKind.StateView);
        _request = new(_model, _executionPlan, profile, new(ArtifactRenderScopeKind.Application, _model.Application.Id));
        _planner = new CratisArtifactRenderPlanner();
    }

    /// <summary>
    /// Compiles one physical source form of the canonical corpus through the real identity-catalog serializer -
    /// never by copying strings or repository-relative files.
    /// </summary>
    /// <param name="form">The physical source form to compile.</param>
    /// <returns>The resulting semantic compilation.</returns>
    protected static SemanticCompilation Compile(CanonicalCorpusSourceForm form)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
            catalog.ResolveDocument(document.StableKey),
            document.StableKey,
            document.DisplayPath,
            document.Text));
        var compilation = new SemanticModelCompiler().Compile(
            Corpus.ApplicationName,
            SemanticDocumentSet.Create([.. documents], catalog));
        compilation.Success.ShouldBeTrue();
        return compilation.Value!;
    }

    protected static string Text(PlannedArtifact artifact) => Encoding.UTF8.GetString(artifact.Bytes.AsSpan());
}
