// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_semantic_exclusions;

public class and_checking_comment_collisions : given.an_excluded_feature
{
    Exception? _failure;

    void Because()
    {
        var comment = SemanticExclusionComments.For(_request).Single();
        var existing = PlannedArtifact.CreateText(comment.RelativePath, "// Existing artifact\n", [_feature.Id]);
        _failure = Catch.Exception(() => ArtifactRenderPlan.Create(_request, [existing, comment], []));
    }

    [Fact] void should_use_the_normal_artifact_collision_rejection() => _failure.ShouldBeOfExactType<InvalidArtifactRenderContract>();
}
