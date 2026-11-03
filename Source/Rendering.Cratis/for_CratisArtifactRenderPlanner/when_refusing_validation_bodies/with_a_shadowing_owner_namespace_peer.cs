// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_refusing_validation_bodies;

public class with_a_shadowing_owner_namespace_peer
{
    [Fact]
    void should_refuse_an_event_that_would_rebind_the_audited_math_type()
    {
        const string body = "return Math.Abs(context.Value) > 0;";
        var ordinary = opaque_policy_model.Plan(validation_body_model.Load(validation_body_model.Command(body)));
        Assert.True(ordinary.Success, opaque_policy_model.Errors(ordinary));
        var source = validation_body_model.Command(body).Replace("OrderPlaced", "Math", StringComparison.Ordinal);
        var plan = opaque_policy_model.Plan(validation_body_model.Load(source));
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-022" && diagnostic.Message.Contains("Math", StringComparison.Ordinal));
    }
}
#endif
