// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticImplementationAdmission.when_admitting_performers.given;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateViewArtifactRenderer.given;

public class a_live_performed_query_application : Specification
{
    private protected SemanticApplicationContext _context = null!;
    protected ImmutableArray<ArtifactRenderDiagnostic> _diagnostics;
    protected string _source = string.Empty;

    protected async Task PrepareLive()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(a_performer.Source("return readModels;"));
        _context = new(a_performer.Request(loaded), new("Projects", "Projects"));
        _context.ScopeFlatProjections();
    }

    protected void Evaluate() => _diagnostics = [.. SemanticCratisAdmission.Evaluate(_context, _context.SelectedSlices())];

    protected void RenderLive() => _source = SemanticStateViewArtifactRenderer.Render(_context.SelectedSlices().Single(located => located.Slice.ReadModels.Length > 0), _context).Content;
}
