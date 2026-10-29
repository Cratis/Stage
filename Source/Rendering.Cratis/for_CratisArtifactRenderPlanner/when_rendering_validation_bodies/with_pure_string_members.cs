// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_validation_bodies;

public class with_pure_string_members
{
    [Theory]
    [InlineData("context.Value.Contains(\"a\")")]
    [InlineData("context.Value.Contains('a')")]
    [InlineData("context.Value.StartsWith(\"a\")")]
    [InlineData("context.Value.EndsWith(\"a\")")]
    [InlineData("context.Value.Contains(\"a\", StringComparison.Ordinal)")]
    [InlineData("context.Value.Contains(\"a\", StringComparison.OrdinalIgnoreCase)")]
    [InlineData("context.Value.StartsWith(\"a\", StringComparison.Ordinal)")]
    [InlineData("context.Value.StartsWith(\"a\", StringComparison.OrdinalIgnoreCase)")]
    [InlineData("context.Value.EndsWith(\"a\", StringComparison.Ordinal)")]
    [InlineData("context.Value.EndsWith(\"a\", StringComparison.OrdinalIgnoreCase)")]
    [InlineData("context.Value.Length > 0")]
    [InlineData("string.IsNullOrWhiteSpace(context.Value)")]
    [InlineData("context.Value.All(char.IsDigit)")]
    public void should_admit_the_pure_member(string expression)
    {
        var plan = opaque_policy_model.Plan(validation_body_model.Load(validation_body_model.Command($"return {expression};").Replace("amount Decimal", "amount String", StringComparison.Ordinal)));
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        opaque_policy_model.Compile(plan);
    }
}
#endif
