// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan;

public class when_inputs_change_without_changing_the_output : given.an_artifact_render_request
{
    ArtifactRenderPlan _original = null!;
    ArtifactRenderPlan _changed = null!;
    void Establish() => _original = ArtifactRenderPlan.Create(_request, [PlannedArtifact.CreateText("same.cs", "same")], []);
    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("source"), "source", "source.play", "concept Unused : String");
        var model = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog)).Value!.Model;
        var request = _request with { Model = model, ExecutionPlan = SemanticExecutionPlan.Compile(model).Plan!, Scope = new(ArtifactRenderScopeKind.Application, model.Application.Id) };
        _changed = ArtifactRenderPlan.Create(request, [PlannedArtifact.CreateText("same.cs", "same")], []);
    }
    [Fact] void should_change_the_semantic_revision() => _changed.SemanticRevision.ShouldNotEqual(_original.SemanticRevision);
    [Fact] void should_preserve_the_output_digest() => _changed.Digest.ShouldEqual(_original.Digest);
}
