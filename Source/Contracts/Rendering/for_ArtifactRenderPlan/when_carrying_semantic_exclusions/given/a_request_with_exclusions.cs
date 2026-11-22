// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_carrying_semantic_exclusions.given;

public class a_request_with_exclusions : for_ArtifactRenderPlan.given.an_artifact_render_request
{
    protected SemanticExclusion _slice = null!;
    protected SemanticExclusion _specification = null!;
    protected ArtifactRenderPlan _result = null!;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var source = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "model.play", "module Projects\n  feature Registration\n    slice StateChange Kept\n");
        var model = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([source], catalog)).Value!.Model;
        _request = _request with { Model = model, ExecutionPlan = SemanticExecutionPlan.Compile(model).Plan, Scope = new(ArtifactRenderScopeKind.Application, model.Application.Id) };
        var parent = model.Application.Modules[0].Features[0].Id;
        _slice = new(SemanticExclusionKind.Slice, "Projects/Registration/Broken", parent, new(4, 2, "model.play"),
            [Diagnostic.Error("PLAY0053", "Unknown event.", new(6, 3, "model.play")), Diagnostic.Error("PLAY0070", "Missing read model.", new(7, 3, "model.play"))]);
        _specification = new(SemanticExclusionKind.Specification, "Projects/Registration/Kept/Example", parent, new(12, 1),
            [Diagnostic.Error("PLAY0053", "Unknown example.", new(12, 1))]) { Cause = SemanticExclusionCause.Dependent, Reason = "Needs the excluded slice.", DependsOn = [new(_slice.Kind, _slice.Address)] };
        _request = _request with { Exclusions = [_slice, _specification] };
    }

    protected ArtifactRenderPlan Create(ImmutableArray<ArtifactRenderDiagnostic> diagnostics = default) =>
        ArtifactRenderPlan.Create(_request, [], diagnostics.IsDefault ? [] : diagnostics);
}
