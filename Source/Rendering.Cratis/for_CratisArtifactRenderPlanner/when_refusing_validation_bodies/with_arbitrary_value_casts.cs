// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_refusing_validation_bodies;

public class with_arbitrary_value_casts
{
    [Theory]
    [InlineData("String", "return (object)context.Value != null;")]
    [InlineData("String", "return (int)context.Value > 0;")]
    [InlineData("String", "return (string)context.Artifact != null;")]
    [InlineData("String", "return context.Value is IDisposable;")]
    [InlineData("String", "return (DateTime)context.Value == default;")]
    [InlineData("String", "return context.ValueAs<object>() != null;")]
    [InlineData("String", "return context.ValueAs<int>() > 0;")]
    [InlineData("Int", "return context.Value is int;")]
    [InlineData("Int[]", "return context.ValueAs<IEnumerable<decimal>>().Sum() > 0;")]
    [InlineData("Int", "return (int)context.Artifact.Amount > 0;")]
    public void should_refuse_the_unrelated_target(string type, string body)
    {
        var plan = opaque_policy_model.Plan(validation_body_model.LoadWholeNumbers(validation_body_model.Command(body).Replace("amount Decimal", $"amount {type}", StringComparison.Ordinal)));
        Assert.False(plan.Success);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-022");
    }
}
#endif
