// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.given;

public class a_scoped_from_block : Specification
{
    private protected SemanticApplicationContext _context = null!;
    protected SemanticProjectionScope _scope = null!;
    protected SemanticProjectionFrom _from = null!;
    protected SemanticProperty _name = null!;
    protected SemanticProperty _info = null!;
    protected SemanticProperty _notes = null!;
    protected SemanticId _nestedName;
    protected SemanticId _eventName;
    protected SemanticId _missing = SemanticId.Create(SemanticKind.Property, "missing");
    protected SemanticProjectionMapping _mapping = null!;
    protected ArtifactRenderDiagnostic _diagnostic = null!;
    SemanticSlice _view = null!;
    SemanticProjection _projection = null!;
    protected bool _child;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("scopes"), "scopes", "Scopes.play", when_rendering_scoped_projections.ScopedSource);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        var options = new CratisRenderingOptions("Projects", "Projects");
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile("Projects", options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        _context = new(request, options);
        _view = _context.SelectedSlices().Single(located => located.Slice.Kind == SemanticSliceKind.StateView).Slice;
        _projection = _view.Projections.Single(projection => projection.Name == "ProjectSummaryProjection");
        var properties = _view.ReadModels.Single(readModel => readModel.Id == _projection.ReadModel).Properties;
        _name = properties.Single(property => property.Name == "name");
        _info = properties.Single(property => property.Name == "info");
        _notes = properties.Single(property => property.Name == "notes");
        _nestedName = _context.Types[_info.Type.Target].Properties.Single().Id;
        _scope = _projection.Scope!;
        _from = _scope.From[1];
        _mapping = _from.Mappings.Single(mapping => mapping.Target[0] == _name.Id);
        _eventName = ((SemanticProjectionEventProperty)_mapping.Source!).Path[0];
    }

    protected void Evaluate()
    {
        var scope = SemanticProjectionScope.Empty with { From = [_scope.From[0], _from] };
        if (_child)
        {
            scope = SemanticProjectionScope.Empty with
            {
                Children = [_scope.Children.Single() with { Scope = SemanticProjectionScope.Empty with { From = [_from] } }]
            };
        }
        var projection = _projection with { Scope = scope };
        var slice = _view with { Projections = [.. _view.Projections.Select(value => value.Id == projection.Id ? projection : value)] };
        var located = _context.Slice(slice.Id) with { Slice = slice };
        _diagnostic = SemanticCratisAdmission.Evaluate(_context, [located]).Single(diagnostic => diagnostic.Code == "STAGE-ESM-017" && diagnostic.Artifact == projection.Id);
    }

    protected void Map(SemanticProjectionMapping mapping) => _from = _from with { Mappings = [mapping] };
    protected void ShouldExplain(string reason) => _diagnostic.Message.ShouldContain(reason);
}
