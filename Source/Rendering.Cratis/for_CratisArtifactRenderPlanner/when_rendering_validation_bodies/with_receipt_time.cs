// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_validation_bodies;

public class with_receipt_time
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void should_require_a_controlled_clock_for_generated_specs(bool controlled)
    {
        var source = validation_body_model.Command("return (DateTimeOffset)context.Value > context.Occurred;").Replace("amount Decimal", "amount DateTime", StringComparison.Ordinal) +
            "\n      specification Future\n" + (controlled ? "        given clock \"2026-06-01T09:00:00Z\"\n" : string.Empty) + """
                    when PlaceOrder
                      orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                      amount = "2026-06-02T09:00:00Z"
                    then OrderPlaced
                      for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                      amount = "2026-06-02T09:00:00Z"
            """;
        var plan = opaque_policy_model.Plan(validation_body_model.Load(source));
        if (!controlled)
        {
            Assert.True(plan.Success, opaque_policy_model.Errors(plan));
            Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Message.Contains("given clock", StringComparison.Ordinal));
            Assert.DoesNotContain(plan.Artifacts, artifact => artifact.RelativePath.Contains("when_future", StringComparison.Ordinal));
            return;
        }
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        var receipt = Assert.Single(plan.Artifacts, artifact => artifact.RelativePath.EndsWith("CommandReceiptTime.cs", StringComparison.Ordinal));
        var text = System.Text.Encoding.UTF8.GetString(receipt.Bytes.AsSpan());
        Assert.Contains("ReceivedAt", text, StringComparison.Ordinal);
        Assert.DoesNotContain("UtcNow", text, StringComparison.Ordinal);
        Assert.Contains(plan.Artifacts, artifact => System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Contains("OccurredAtReceipt(new", StringComparison.Ordinal));
        opaque_policy_model.Compile(plan);
    }
}
#endif
