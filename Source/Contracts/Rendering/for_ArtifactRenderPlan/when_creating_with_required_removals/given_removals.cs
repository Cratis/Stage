// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_creating_with_required_removals;

public class given_removals : given.an_artifact_render_request
{
    protected const string Hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
    protected ImmutableArray<PlannedArtifact> _artifacts;
    protected SemanticId _model;

    void Establish()
    {
        _model = _request.Model.Application.Id;
        _artifacts = [PlannedArtifact.CreateText("Orders/Totals/Totals.cs", "model", [_model])];
    }

    protected ArtifactRenderPlan Create(params ArtifactRemoval[] removals) =>
        ArtifactRenderPlan.Create(_request, _artifacts, [], [], [], [.. removals]);
}
