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
    ArtifactRenderPlan _singleFormPlan = null!;
    ArtifactRenderPlan[] _otherFormPlans = [];

    void Because()
    {
        var scope = new ArtifactRenderScope(ArtifactRenderScopeKind.Application, _model.Application.Id);
        _singleFormPlan = CratisRendering.Plan(_model, _executionPlan, scope, _options);

        // Every other physical arrangement of the same model - split into folders, reordered, relocated - must
        // plan exactly what the single document plans.
        _otherFormPlans = [.. Corpus.SourceForms.Where(_ => _.Name != "single").Select(form =>
        {
            var model = Compile(form).Model;
            return CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, scope, _options);
        })];
    }

    [Fact] void should_keep_the_stable_corpus_identity() => Corpus.Name.ShouldEqual("register-project/v1-legacy");
    [Fact] void should_keep_the_fixed_application_identity() => Corpus.ApplicationIdentity.ToString().ShouldEqual("app1:20ccb167f2400bc55fae1597b1a0f4d19b40841f513bd013a7fa815e9e7f2994");
    [Fact] void should_expose_every_physical_source_form() => Corpus.SourceForms.Select(_ => _.Name).ShouldEqual("single", "folder", "reordered", "relocated");
    [Fact] void should_keep_distinct_document_catalogs_for_distinct_physical_forms() => Corpus.SourceForms[0].IdentityCatalogBytes.SequenceEqual(Corpus.SourceForms[1].IdentityCatalogBytes).ShouldBeFalse();
    [Fact] void should_load_the_single_form_at_the_expected_semantic_revision() => _model.Revision.ShouldEqual(Corpus.SemanticRevision);
    [Fact] void should_all_succeed() => _otherFormPlans.Append(_singleFormPlan).All(_ => _.Success).ShouldBeTrue();
    [Fact] void should_plan_identical_artifact_paths_for_every_physical_form() => _otherFormPlans.All(plan => plan.Artifacts.Select(_ => _.RelativePath).SequenceEqual(_singleFormPlan.Artifacts.Select(_ => _.RelativePath))).ShouldBeTrue();
    [Fact] void should_plan_identical_artifact_bytes_for_every_physical_form() => _otherFormPlans.All(plan => plan.Artifacts.Length == _singleFormPlan.Artifacts.Length && plan.Artifacts.Zip(_singleFormPlan.Artifacts).All(pair => pair.First.Bytes.SequenceEqual(pair.Second.Bytes))).ShouldBeTrue();
    [Fact] void should_plan_identical_artifact_hashes_for_every_physical_form() => _otherFormPlans.All(plan => plan.Artifacts.Select(_ => _.Sha256).SequenceEqual(_singleFormPlan.Artifacts.Select(_ => _.Sha256))).ShouldBeTrue();
}
