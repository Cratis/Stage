// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_validation_bodies;

public class with_a_bare_value_type_test : Specification
{
    [Theory]
    [InlineData("Int", "long")]
    [InlineData("Decimal", "decimal")]
    [InlineData("Uuid", "Guid")]
    public void should_refuse_the_always_true_test_without_a_warning(string type, string realization)
    {
        var source = validation_body_model.Command($"return context.Value is {realization};")
            .Replace("amount Decimal", $"amount {type}", StringComparison.Ordinal);
        var plan = opaque_policy_model.Plan(validation_body_model.LoadWholeNumbers(source));
        Assert.False(plan.Success);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-022" && diagnostic.Message.Contains("always true", StringComparison.Ordinal));
        Assert.DoesNotContain(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-019");
    }
}
#endif
