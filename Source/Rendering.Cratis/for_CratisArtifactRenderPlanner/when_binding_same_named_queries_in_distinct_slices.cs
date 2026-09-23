// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_binding_same_named_queries_in_distinct_slices : a_register_project_render_request
{
    ArtifactRenderPlan _plan = null!;
    string _bindings = null!;
    string[] _identities = null!;

    void Establish()
    {
        var form = Corpus.SourceForms.Single(_ => _.Name == "single");
        var original = form.Documents.Single();
        const string AdditionalSlice = """
                slice StateView OtherLookup
                  readmodel OtherSummary
                    projectId ProjectId
                    name ProjectName
                  query OtherById => OtherSummary?
                    by projectId ProjectId
                  projection OtherSummaryProjection => OtherSummary
                    from ProjectRegistered key projectId
                      projectId = projectId
                      name = name
            """;
        _model = Compile(form with { Documents = [original with { Bytes = [.. Encoding.UTF8.GetBytes(original.Text + Environment.NewLine + AdditionalSlice)] }] }).Model;

        // The textual ESM v1 binder rejects duplicate query names today. Exercise the renderer's identity
        // contract with distinct, validated semantic sources rather than depending on that binder limitation.
        var module = _model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slices = feature.Slices.Select(slice => slice.Name == "OtherLookup"
            ? slice with { Queries = [slice.Queries.Single() with { Name = "ProjectById" }] }
            : slice);
        var application = _model.Application with { Modules = [module with { Features = [feature with { Slices = [.. slices] }] }] };
        _model = ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, application);
        _executionPlan = SemanticExecutionPlan.Compile(_model).Plan!;
        _request = new(_model, _executionPlan, _request.Profile, new(ArtifactRenderScopeKind.Application, _model.Application.Id));
        _identities = [.. _model.Application.Modules.Single().Features.Single().Slices.SelectMany(_ => _.Queries).Select(_ => _.Id.ToString()).Order(StringComparer.Ordinal)];
    }

    void Because()
    {
        _plan = _planner.Plan(_request);
        _bindings = Text(_plan.Artifacts.Single(_ => _.RelativePath == SceneBindingsRenderer.RelativePath));
    }

    [Fact] void should_admit_the_distinct_sources() => _plan.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_both_query_sources() => _identities.Distinct(StringComparer.Ordinal).Count().ShouldEqual(2);
    [Fact] void should_register_every_identity_once() => _identities.All(identity => _bindings.Split($"registerQueryIdentity(\"ProjectById\", {JsonSerializer.Serialize(identity)},", StringSplitOptions.None).Length == 2).ShouldBeTrue();
    [Fact] void should_alias_the_two_imports_without_collapsing_them() => new[] { "ProjectById as __sceneQuery0", "ProjectById as __sceneQuery1" }.All(_bindings.Contains).ShouldBeTrue();
    [Fact] void should_import_both_actual_source_folders() => new[] { "from '../Projects/Registration/ProjectLookup'", "from '../Projects/Registration/OtherLookup'" }.All(_bindings.Contains).ShouldBeTrue();
    [Fact] void should_not_add_a_legacy_registration() => _bindings.ShouldNotContain("registerQueries");
    [Fact] void should_not_compose_a_permanently_ambiguous_lookup() => Text(_plan.Artifacts.Single(_ => _.RelativePath == SceneCompositionInput.RelativePath)).ShouldNotContain("queryInputForm");
    [Fact] void should_keep_deterministic_registration_bytes() => Text(_planner.Plan(_request).Artifacts.Single(_ => _.RelativePath == SceneBindingsRenderer.RelativePath)).ShouldEqual(_bindings);
}
