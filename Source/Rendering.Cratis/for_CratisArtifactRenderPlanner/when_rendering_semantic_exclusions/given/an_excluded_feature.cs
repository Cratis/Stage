// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_semantic_exclusions.given;

public class an_excluded_feature : for_CratisArtifactRenderPlanner.given.a_register_project_render_request
{
    protected SemanticExclusion _exclusion = null!;
    protected ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        _exclusion = new(
            SemanticExclusionKind.Slice,
            "Projects/Registration/Broken",
            _feature.Id,
            new(4, 2, "model.play"),
            [Diagnostic.Error("PLAY0053", "Unknown event.", new(6, 3, "model.play"))]);
        _request = _request with { Exclusions = [_exclusion] };
    }

    protected PlannedArtifact Comment => _plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("ScreenplayExclusions.g.cs", StringComparison.Ordinal));
    protected PlannedArtifact Report => _plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("UNSUPPORTED.md", StringComparison.Ordinal));
}
