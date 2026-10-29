// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_validation_bodies;

public class with_an_unused_receipt_time_concept : Specification
{
    ArtifactRenderPlan _plan = null!;

    internal static string Source => validation_body_model.Concept("return context.Value > context.Occurred;")
        .Replace("concept Amount : Decimal", "concept Amount : DateTime", StringComparison.Ordinal)
        .Replace("amount Amount", "amount Decimal", StringComparison.Ordinal);

    void Because() => _plan = opaque_policy_model.Plan(validation_body_model.Load(Source));

    [Fact]
    public void should_emit_the_receipt_helper_for_the_rendered_concept_validator()
    {
        Assert.True(_plan.Success, opaque_policy_model.Errors(_plan));
        Assert.Single(_plan.Artifacts, artifact => artifact.RelativePath.EndsWith("CommandReceiptTime.cs", StringComparison.Ordinal));
        opaque_policy_model.Compile(_plan);
    }

    [Fact]
    public void should_reserve_the_receipt_helper_namespace()
    {
        var source = Source.Replace("module Orders", "module GeneratedCommands", StringComparison.Ordinal)
            .Replace("feature Ordering", "feature CommandReceiptTime", StringComparison.Ordinal);
        var plan = opaque_policy_model.Plan(validation_body_model.Load(source));
        Assert.False(plan.Success);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Message.Contains("CommandReceiptTime", StringComparison.Ordinal));
    }
}
#endif
