// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_validation_bodies;

public class with_value_accessors
{
    [Theory]
    [InlineData("String", "return (string)context.Value == context.Value;")]
    [InlineData("Int", "return (int)context.Value > 0;")]
    [InlineData("Int", "return (long)context.Value > 0;")]
    [InlineData("String", "return context.ValueAs<string>().Length > 0;")]
    [InlineData("Bool", "return (bool)context.Value;")]
    [InlineData("Decimal", "return (decimal)context.Value >= 0;")]
    [InlineData("Uuid", "return (Guid)context.Value == context.Value;")]
    [InlineData("Date", "return (DateOnly)context.Value == context.Value;")]
    [InlineData("DateTime", "return (DateTimeOffset)context.Value == context.Value;")]
    [InlineData("Int", "return context.ValueAs<int>() > 0;")]
    [InlineData("Int[]", "return context.ValueAs<IEnumerable<int>>().Sum() > 0;")]
    [InlineData("Int[]", "return context.ValueAs<IEnumerable<long>>().Sum() > 0;")]
    [InlineData("Decimal", "return context.ArtifactAs<PlaceOrder>().Amount > 0 && !context.IsWholeArtifact;")]
    public void should_admit_the_declared_realization(string type, string body)
    {
        var source = validation_body_model.Command(body).Replace("amount Decimal", $"amount {type}", StringComparison.Ordinal);
        var plan = opaque_policy_model.Plan(validation_body_model.LoadWholeNumbers(source));
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        opaque_policy_model.Compile(plan);
    }
}
#endif
