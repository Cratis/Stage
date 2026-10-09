// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.given;

public class a_query_shape : a_register_project_render_request
{
    protected SemanticKeyedQuery _query = null!;
    protected SemanticSlice _slice = null!;
    private protected SemanticApplicationContext _context = null!;
    protected ArtifactRenderDiagnostic[] _diagnostics = [];

    void Establish()
    {
        _query = _projectLookup.Queries.Single();
        _slice = _projectLookup;
    }

    protected void Configure(SemanticQueryCardinality cardinality, SemanticQueryDelivery delivery, bool keyed)
    {
        _query = _query with
        {
            Cardinality = cardinality,
            Delivery = delivery,
            Argument = keyed ? _query.Argument : null,
            KeyProperty = keyed ? _query.KeyProperty : null
        };
        _slice = _slice with { Queries = [_query], Specifications = [] };
    }

    private protected SemanticApplicationContext Context()
    {
        var feature = _feature with { Slices = [_registerProject with { Specifications = [] }, _slice] };
        var module = _module with { Features = [feature] };
        var model = ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, _model.Application with { Modules = [module] });
        return new(_request with { Model = model }, _options);
    }

    protected void Evaluate()
    {
        _context = Context();
        _diagnostics = [.. SemanticCratisAdmission.Evaluate(_context, [_context.Slice(_slice.Id)])];
    }
}
