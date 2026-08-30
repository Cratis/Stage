// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_the_canonical_corpus_physical_forms : a_register_project_render_request
{
    SemanticExecutionPlan _folderExecutionPlan = null!;
    ArtifactRenderPlan _singleFormPlan = null!;
    ArtifactRenderPlan _folderFormPlan = null!;

    void Because()
    {
        var folderForm = Corpus.SourceForms.Single(_ => _.Name == "folder");
        var folderModel = Compile(folderForm).Model;
        _folderExecutionPlan = SemanticExecutionPlan.Compile(folderModel).Plan!;

        var scope = new ArtifactRenderScope(ArtifactRenderScopeKind.Application, _model.Application.Id);
        _singleFormPlan = CratisRendering.Plan(_model, _executionPlan, scope, _options);
        _folderFormPlan = CratisRendering.Plan(folderModel, _folderExecutionPlan, scope, _options);
    }

    [Fact] void should_keep_the_stable_corpus_identity() => Corpus.Name.ShouldEqual("register-project/v1-legacy");
    [Fact] void should_keep_the_fixed_application_identity() => Corpus.ApplicationIdentity.ToString().ShouldEqual("app1:20ccb167f2400bc55fae1597b1a0f4d19b40841f513bd013a7fa815e9e7f2994");
    [Fact] void should_expose_single_and_folder_source_forms() => Corpus.SourceForms.Select(_ => _.Name).ShouldEqual("single", "folder");
    [Fact] void should_keep_distinct_document_catalogs_for_distinct_physical_forms() => Corpus.SourceForms[0].IdentityCatalogBytes.SequenceEqual(Corpus.SourceForms[1].IdentityCatalogBytes).ShouldBeFalse();
    [Fact] void should_load_the_single_form_at_the_expected_semantic_revision() => _model.Revision.ShouldEqual(Corpus.SemanticRevision);
    [Fact] void should_both_succeed() => (_singleFormPlan.Success && _folderFormPlan.Success).ShouldBeTrue();
    [Fact] void should_plan_identical_artifact_paths_for_both_physical_forms() => _folderFormPlan.Artifacts.Select(_ => _.RelativePath).SequenceEqual(_singleFormPlan.Artifacts.Select(_ => _.RelativePath)).ShouldBeTrue();
    [Fact] void should_plan_identical_artifact_bytes_for_both_physical_forms() => _folderFormPlan.Artifacts.Zip(_singleFormPlan.Artifacts).All(pair => pair.First.Bytes.SequenceEqual(pair.Second.Bytes)).ShouldBeTrue();
    [Fact] void should_plan_identical_artifact_hashes_for_both_physical_forms() => _folderFormPlan.Artifacts.Select(_ => _.Sha256).SequenceEqual(_singleFormPlan.Artifacts.Select(_ => _.Sha256)).ShouldBeTrue();
}
