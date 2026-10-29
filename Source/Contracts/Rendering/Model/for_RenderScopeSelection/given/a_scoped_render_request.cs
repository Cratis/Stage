// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderScopeSelection.given;

public class a_scoped_render_request : Specification
{
    const string Source = """
        module Banking
          feature Accounts
            slice StateChange Open
              event Opened
            slice StateChange Close
              event Closed
            feature Savings
              slice StateChange Save
                event Saved
        module Shipping
          feature Parcels
            slice StateChange Send
              event Sent
        """;

    protected ArtifactRenderRequest _request = null!;
    protected SemanticApplication _application = null!;
    protected SemanticModule _banking = null!;
    protected SemanticFeature _accounts = null!;
    protected SemanticFeature _savings = null!;

    protected SemanticModule Shipping => _application.Modules[1];

    protected IReadOnlyList<string> Selected(ArtifactRenderRequest request) => [.. RenderScopeSelection.Select(request).Select(slice => slice.Slice.Name)];

    protected ArtifactRenderRequest With(ArtifactRenderScopeKind kind, SemanticId artifact) => _request with { Scope = new(kind, artifact) };

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("scopes"), "scopes", "scopes.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        _application = model.Application;
        _banking = _application.Modules[0];
        _accounts = _banking.Features[0];
        _savings = _accounts.Features[0];
        var profile = ArtifactRenderProfile.Create("cratis", "1", "Cratis.Stage.Rendering.Cratis", "1", []);
        _request = new(model, SemanticExecutionPlan.Compile(model).Plan!, profile, new(ArtifactRenderScopeKind.Application, _application.Id));
    }
}
