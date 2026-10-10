// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan;

public class when_refusing_an_unknown_additional_scope : given.an_artifact_render_request
{
    Exception? _error;
    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("source"), "source", "source.play", """
            concept ProjectId : Uuid
            module Projects
              feature Registration
                slice StateChange RegisterProject
                  command RegisterProject
                    projectId ProjectId identifier
                    produces ProjectRegistered
                      projectId = projectId
                  event ProjectRegistered
                    projectId ProjectId
            """);
        var model = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog)).Value!.Model;
        _request = new(model, SemanticExecutionPlan.Compile(model).Plan!, _request.Profile, new(ArtifactRenderScopeKind.Module, model.Application.Modules.Single().Id));
    }
    void Because() => _error = Catch.Exception(() => ArtifactRenderPlan.Create(_request with { AdditionalScopes = [new(ArtifactRenderScopeKind.Slice, _request.Model.Application.Id)] }, [], []));
    [Fact] void should_validate_every_scope_against_the_model() => _error.ShouldBeOfExactType<InvalidArtifactRenderContract>();
}
