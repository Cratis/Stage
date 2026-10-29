// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reaction_appends;

public class with_a_native_body_hitting_an_append_constraint
{
    [Fact]
    public void should_omit_the_reaction_specs()
    {
        var source = native_reaction_model.Source().Replace("      specification HighScoreHighlights", "      constraint UniqueHighlightScore\n        unique score on TalkHighlighted\n      specification HighScoreHighlights", StringComparison.Ordinal);
        var plan = opaque_policy_model.Plan(reaction_body_model.LoadSource(source));
        var omissions = plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-ESM-011").ToArray();
        Assert.Equal(2, omissions.Length);
        Assert.All(omissions, diagnostic => Assert.Contains("append-time constraints", diagnostic.Message, StringComparison.Ordinal));
    }
}
#endif
