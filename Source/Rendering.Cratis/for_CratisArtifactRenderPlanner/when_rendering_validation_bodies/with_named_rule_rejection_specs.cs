// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_validation_bodies;

public class with_named_rule_rejection_specs
{
    [Fact]
    public void should_emit_the_rejection_assertion()
    {
        var source = validation_body_model.Command("return context.Value > 0;") + """

                  specification RejectingZero
                    when PlaceOrder
                      orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                      amount = 0
                    then error "Positive amount required"
            """;
        var plan = opaque_policy_model.Plan(validation_body_model.Load(source));
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        Assert.DoesNotContain(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-011");
        Assert.Contains(plan.Artifacts, artifact => System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Contains("Positive amount required", StringComparison.Ordinal) && artifact.RelativePath.Contains("when_rejecting_zero", StringComparison.Ordinal));
    }
}
#endif
