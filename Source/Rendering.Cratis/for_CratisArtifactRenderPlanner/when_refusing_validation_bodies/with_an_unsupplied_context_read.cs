// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_refusing_validation_bodies;

public class with_an_unsupplied_context_read
{
    [Theory]
    [InlineData("Tenant")]
    [InlineData("CausedBy")]
    public void should_refuse_the_read_by_member_name(string member)
    {
        var plan = opaque_policy_model.Plan(validation_body_model.Load(validation_body_model.Command($"return context.{member} == default;")));
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-015" && diagnostic.Message.Contains(member, StringComparison.Ordinal));
    }
}
#endif
