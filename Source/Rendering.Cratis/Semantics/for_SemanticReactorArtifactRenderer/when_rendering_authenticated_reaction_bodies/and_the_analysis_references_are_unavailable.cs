// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

public class and_the_analysis_references_are_unavailable : a_rendered_reaction_body
{
    void Because()
    {
        var previous = PureTransitionAdmission.ReferenceDirectoryOverride.Value;
        PureTransitionAdmission.ReferenceDirectoryOverride.Value = Path.Combine(Path.GetTempPath(), $"absent-stage-ref-{Guid.NewGuid():N}");
        try
        {
            Render(reaction_body_model.Source("return null;"));
        }
        finally
        {
            PureTransitionAdmission.ReferenceDirectoryOverride.Value = previous;
        }
    }

    [Fact] void should_refuse_without_publishing_artifacts() => ShouldRefuseWith("analysis references unavailable");
}
#endif
