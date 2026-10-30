// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_refusing_validation_bodies;

public class with_impure_string_members
{
    [Theory]
    [InlineData("context.Value.StartsWith(\"a\", StringComparison.CurrentCulture)")]
    [InlineData("context.Value.EndsWith(\"a\", StringComparison.InvariantCulture)")]
    [InlineData("context.Value.ToUpper() == \"A\"")]
    [InlineData("string.Format(\"{0}\", context.Value) == \"a\"")]
    [InlineData("context.Value.Normalize() == \"a\"")]
    public void should_refuse_the_impure_member(string expression)
    {
        var plan = opaque_policy_model.Plan(validation_body_model.Load(validation_body_model.Command($"return {expression};").Replace("amount Decimal", "amount String", StringComparison.Ordinal)));
        Assert.False(plan.Success);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-022");
    }
}
#endif
