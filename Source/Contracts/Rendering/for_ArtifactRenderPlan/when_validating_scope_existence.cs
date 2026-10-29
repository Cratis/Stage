// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan;

public class when_validating_scope_existence : given.an_artifact_render_request
{
    ExecutableSemanticModel _model = null!;
    SemanticModule _module = null!;
    SemanticFeature _nested = null!;
    SemanticSlice _nestedSlice = null!;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("source"), "source", "source.play", """
            concept ProjectId : Uuid
            module Projects
              feature Registration
                feature Approvals
                  slice StateChange ApproveProject
                    command ApproveProject
                      projectId ProjectId identifier
                      produces ProjectApproved
                        projectId = projectId
                    event ProjectApproved
                      projectId ProjectId
            """);
        _model = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog)).Value!.Model;
        _module = _model.Application.Modules.Single();
        _nested = _module.Features.Single().Features.Single();
        _nestedSlice = _nested.Slices.Single();
    }

    Exception? Create(ArtifactRenderScope scope) =>
        Catch.Exception(() => ArtifactRenderPlan.Create(new(_model, SemanticExecutionPlan.Compile(_model).Plan!, _request.Profile, scope), [], []));

    [Fact] void should_accept_the_application() => Create(new(ArtifactRenderScopeKind.Application, _model.Application.Id)).ShouldBeNull();
    [Fact] void should_accept_a_module() => Create(new(ArtifactRenderScopeKind.Module, _module.Id)).ShouldBeNull();
    [Fact] void should_accept_a_nested_feature() => Create(new(ArtifactRenderScopeKind.Feature, _nested.Id)).ShouldBeNull();
    [Fact] void should_accept_a_slice_in_a_nested_feature() => Create(new(ArtifactRenderScopeKind.Slice, _nestedSlice.Id)).ShouldBeNull();
    [Fact] void should_refuse_another_application() => Create(new(ArtifactRenderScopeKind.Application, _module.Id)).ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_a_feature_as_a_module() => Create(new(ArtifactRenderScopeKind.Module, _nested.Id)).ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_a_slice_as_a_feature() => Create(new(ArtifactRenderScopeKind.Feature, _nestedSlice.Id)).ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_an_unknown_kind() => Create(new(ArtifactRenderScopeKind.Unknown, _nestedSlice.Id)).ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_an_undefined_kind() => Create(new((ArtifactRenderScopeKind)42, _nestedSlice.Id)).ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_an_unset_artifact() => Create(new(ArtifactRenderScopeKind.Slice, default)).ShouldBeOfExactType<InvalidArtifactRenderContract>();
}
